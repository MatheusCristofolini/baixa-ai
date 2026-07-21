using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;

[assembly: System.Reflection.AssemblyCompany("Macritec Tecnologia")]
[assembly: System.Reflection.AssemblyProduct("Baixa AI")]
[assembly: System.Reflection.AssemblyCopyright("Copyright © Macritec Tecnologia 2026")]
[assembly: System.Reflection.AssemblyTitle("Baixa AI - Downloader de Mídias")]
[assembly: System.Reflection.AssemblyDescription("Interface gráfica para o motor yt-dlp")]
[assembly: System.Reflection.AssemblyVersion("1.0.0.0")]
[assembly: System.Reflection.AssemblyFileVersion("1.0.0.0")]

namespace BaixaAI
{
    public enum CustomProgressBarStyle
    {
        Blocks,
        Marquee
    }

    public class CustomProgressBar : Panel
    {
        private int _value = 0;
        private Color _progressColor = Color.FromArgb(225, 29, 72); // Rose-red accent
        private CustomProgressBarStyle _style = CustomProgressBarStyle.Blocks;
        private System.Windows.Forms.Timer _marqueeTimer;
        private int _marqueeOffset = 0;

        public int Value
        {
            get { return _value; }
            set
            {
                _value = Math.Min(100, Math.Max(0, value));
                this.Invalidate();
            }
        }

        public Color ProgressColor
        {
            get { return _progressColor; }
            set
            {
                _progressColor = value;
                this.Invalidate();
            }
        }

        public CustomProgressBarStyle Style
        {
            get { return _style; }
            set
            {
                _style = value;
                if (_style == CustomProgressBarStyle.Marquee)
                {
                    StartMarquee();
                }
                else
                {
                    StopMarquee();
                }
                this.Invalidate();
            }
        }

        public CustomProgressBar()
        {
            this.DoubleBuffered = true;
            this.BackColor = Color.FromArgb(31, 41, 55); // Gray-800
            
            _marqueeTimer = new System.Windows.Forms.Timer();
            _marqueeTimer.Interval = 20; // Fast and smooth animation
            _marqueeTimer.Tick += new EventHandler(MarqueeTimer_Tick);
        }

        private void MarqueeTimer_Tick(object sender, EventArgs e)
        {
            _marqueeOffset = (_marqueeOffset + 5) % this.Width;
            this.Invalidate();
        }

        private void StartMarquee()
        {
            if (!_marqueeTimer.Enabled)
            {
                _marqueeTimer.Start();
            }
        }

        private void StopMarquee()
        {
            if (_marqueeTimer.Enabled)
            {
                _marqueeTimer.Stop();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Draw clean border
            using (Pen borderPen = new Pen(Color.FromArgb(75, 85, 99), 1)) // Gray-600
            {
                e.Graphics.DrawRectangle(borderPen, 0, 0, this.Width - 1, this.Height - 1);
            }

            if (_style == CustomProgressBarStyle.Blocks)
            {
                if (_value > 0)
                {
                    int fillWidth = (int)((this.Width - 2) * (_value / 100.0));
                    if (fillWidth > 0)
                    {
                        using (Brush fillBrush = new SolidBrush(_progressColor))
                        {
                            e.Graphics.FillRectangle(fillBrush, 1, 1, fillWidth, this.Height - 2);
                        }
                    }
                }
            }
            else if (_style == CustomProgressBarStyle.Marquee)
            {
                int marqueeWidth = this.Width / 4;
                int x = _marqueeOffset;

                using (Brush fillBrush = new SolidBrush(_progressColor))
                {
                    e.Graphics.FillRectangle(fillBrush, x, 1, marqueeWidth, this.Height - 2);
                    if (x + marqueeWidth > this.Width)
                    {
                        e.Graphics.FillRectangle(fillBrush, 0, 1, (x + marqueeWidth) - this.Width, this.Height - 2);
                    }
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_marqueeTimer != null)
                {
                    _marqueeTimer.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }

    public class MainForm : Form
    {
        // UI Colors
        private static readonly Color ColorBg = Color.FromArgb(17, 24, 39);      // Gray-900
        private static readonly Color ColorCard = Color.FromArgb(31, 41, 55);    // Gray-800
        private static readonly Color ColorBorder = Color.FromArgb(55, 65, 81);  // Gray-700
        private static readonly Color ColorText = Color.FromArgb(243, 244, 246);  // Gray-100
        private static readonly Color ColorTextMuted = Color.FromArgb(156, 163, 175); // Gray-400
        private static readonly Color ColorAccent = Color.FromArgb(225, 29, 72);  // Rose-600
        private static readonly Color ColorAccentHover = Color.FromArgb(244, 63, 94); // Rose-500
        private static readonly Color ColorInputBg = Color.FromArgb(55, 65, 81); // Gray-700

        // Controls
        private TextBox txtUrl;
        private TextBox txtDestFolder;
        private Button btnBrowseDest;
        private ComboBox cmbQuality;
        private CheckBox chkLiveFromStart;
        private CheckBox chkNoPlaylist;
        private CheckBox chkEmbedSubs;
        private CheckBox chkUseCookies;
        private TextBox txtCookiesPath;
        private Button btnBrowseCookies;
        private Button btnDownload;
        
        private Label lblStatus;
        private Label lblPercent;
        private Label lblSpeed;
        private Label lblSize;
        private Label lblEta;
        private CustomProgressBar progressBar;
        
        private CheckBox chkShowLogs;
        private TextBox txtLog;
        private Panel panelLogs;

        // Process management
        private Process currentProcess = null;
        private bool isDownloading = false;

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        public MainForm()
        {
            InitializeComponent();
            LoadConfig();
            this.FormClosing += new FormClosingEventHandler(MainForm_FormClosing);
        }

        private void InitializeComponent()
        {
            this.Text = "Baixa AI - Downloader de Mídias";
            this.Size = new Size(720, 560);
            this.MinimumSize = new Size(680, 520);
            this.BackColor = ColorBg;
            this.ForeColor = ColorText;
            this.Font = new Font("Segoe UI", 9.5F);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Load icon if it exists in the folder
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_icon.ico");
                if (File.Exists(iconPath))
                {
                    this.Icon = new Icon(iconPath);
                }
            }
            catch { }

            // Layout setup
            TableLayoutPanel mainLayout = new TableLayoutPanel();
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.ColumnCount = 1;
            mainLayout.RowCount = 3;
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F)); // Header
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // Form inputs
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // Log console (will grow dynamically)
            this.Controls.Add(mainLayout);

            // ================= HEADER =================
            Panel panelHeader = new Panel();
            panelHeader.Dock = DockStyle.Fill;
            panelHeader.BackColor = Color.FromArgb(24, 31, 46); // Slightly different dark color for header
            panelHeader.Padding = new Padding(20, 10, 20, 10);
            mainLayout.Controls.Add(panelHeader, 0, 0);

            Label lblTitle = new Label();
            lblTitle.Text = "Baixa AI";
            lblTitle.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            lblTitle.ForeColor = ColorAccent;
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(16, 8);
            panelHeader.Controls.Add(lblTitle);

            Label lblSubtitle = new Label();
            lblSubtitle.Text = "Programa para baixar mídias online (Quando a plataforma permitir)";
            lblSubtitle.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            lblSubtitle.ForeColor = ColorTextMuted;
            lblSubtitle.AutoSize = true;
            lblSubtitle.Location = new Point(20, 42);
            panelHeader.Controls.Add(lblSubtitle);

            // ================= FORM BODY =================
            Panel panelBody = new Panel();
            panelBody.Dock = DockStyle.Fill;
            panelBody.Padding = new Padding(20, 10, 20, 15);
            mainLayout.Controls.Add(panelBody, 0, 1);

            // URL input group
            Label lblUrl = new Label();
            lblUrl.Text = "URL do Vídeo, Live ou Mídia Online:";
            lblUrl.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblUrl.Location = new Point(20, 15);
            lblUrl.AutoSize = true;
            panelBody.Controls.Add(lblUrl);

            txtUrl = new TextBox();
            txtUrl.Location = new Point(20, 38);
            txtUrl.Width = 660;
            txtUrl.BackColor = ColorCard;
            txtUrl.ForeColor = ColorText;
            txtUrl.BorderStyle = BorderStyle.FixedSingle;
            txtUrl.Font = new Font("Segoe UI", 11F);
            txtUrl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panelBody.Controls.Add(txtUrl);

            // Save directory group
            Label lblDest = new Label();
            lblDest.Text = "Salvar na pasta:";
            lblDest.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblDest.Location = new Point(20, 80);
            lblDest.AutoSize = true;
            panelBody.Controls.Add(lblDest);

            txtDestFolder = new TextBox();
            txtDestFolder.Location = new Point(20, 103);
            txtDestFolder.Width = 530;
            txtDestFolder.BackColor = ColorCard;
            txtDestFolder.ForeColor = ColorText;
            txtDestFolder.BorderStyle = BorderStyle.FixedSingle;
            txtDestFolder.Font = new Font("Segoe UI", 10F);
            txtDestFolder.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panelBody.Controls.Add(txtDestFolder);

            btnBrowseDest = new Button();
            btnBrowseDest.Text = "Selecionar...";
            btnBrowseDest.Location = new Point(560, 100);
            btnBrowseDest.Size = new Size(120, 28);
            btnBrowseDest.FlatStyle = FlatStyle.Flat;
            btnBrowseDest.FlatAppearance.BorderColor = ColorBorder;
            btnBrowseDest.BackColor = ColorInputBg;
            btnBrowseDest.ForeColor = ColorText;
            btnBrowseDest.Cursor = Cursors.Hand;
            btnBrowseDest.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnBrowseDest.Click += new EventHandler(BtnBrowseDest_Click);
            panelBody.Controls.Add(btnBrowseDest);

            // Quality / Options Column 1
            Label lblQuality = new Label();
            lblQuality.Text = "Qualidade do Download:";
            lblQuality.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblQuality.Location = new Point(20, 145);
            lblQuality.AutoSize = true;
            panelBody.Controls.Add(lblQuality);

            cmbQuality = new ComboBox();
            cmbQuality.Location = new Point(20, 168);
            cmbQuality.Width = 300;
            cmbQuality.BackColor = ColorCard;
            cmbQuality.ForeColor = ColorText;
            cmbQuality.FlatStyle = FlatStyle.Flat;
            cmbQuality.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbQuality.Font = new Font("Segoe UI", 10F);
            cmbQuality.Items.AddRange(new object[] {
                "Melhor Qualidade (Padrão)",
                "1080p (Full HD)",
                "720p (HD)",
                "480p (SD)",
                "Apenas Áudio (MP3)"
            });
            cmbQuality.SelectedIndex = 0;
            panelBody.Controls.Add(cmbQuality);

            // Cookies Group
            chkUseCookies = new CheckBox();
            chkUseCookies.Text = "Usar arquivo de cookies (para vídeos restritos)";
            chkUseCookies.Location = new Point(20, 210);
            chkUseCookies.AutoSize = true;
            chkUseCookies.FlatStyle = FlatStyle.Flat;
            chkUseCookies.FlatAppearance.BorderColor = ColorBorder;
            chkUseCookies.CheckedChanged += new EventHandler(ChkUseCookies_CheckedChanged);
            panelBody.Controls.Add(chkUseCookies);

            txtCookiesPath = new TextBox();
            txtCookiesPath.Location = new Point(20, 238);
            txtCookiesPath.Width = 530;
            txtCookiesPath.BackColor = ColorCard;
            txtCookiesPath.ForeColor = ColorText;
            txtCookiesPath.BorderStyle = BorderStyle.FixedSingle;
            txtCookiesPath.Font = new Font("Segoe UI", 9.5F);
            txtCookiesPath.Enabled = false;
            txtCookiesPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panelBody.Controls.Add(txtCookiesPath);

            btnBrowseCookies = new Button();
            btnBrowseCookies.Text = "Procurar...";
            btnBrowseCookies.Location = new Point(560, 235);
            btnBrowseCookies.Size = new Size(120, 26);
            btnBrowseCookies.FlatStyle = FlatStyle.Flat;
            btnBrowseCookies.FlatAppearance.BorderColor = ColorBorder;
            btnBrowseCookies.BackColor = ColorInputBg;
            btnBrowseCookies.ForeColor = ColorText;
            btnBrowseCookies.Enabled = false;
            btnBrowseCookies.Cursor = Cursors.Hand;
            btnBrowseCookies.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnBrowseCookies.Click += new EventHandler(BtnBrowseCookies_Click);
            panelBody.Controls.Add(btnBrowseCookies);

            // Advanced options (Checkboxes) on the right side
            Label lblAdv = new Label();
            lblAdv.Text = "Opções para Live Stream / Download:";
            lblAdv.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblAdv.Location = new Point(360, 145);
            lblAdv.AutoSize = true;
            panelBody.Controls.Add(lblAdv);

            chkLiveFromStart = new CheckBox();
            chkLiveFromStart.Text = "Baixar live desde o início (--live-from-start)";
            chkLiveFromStart.Location = new Point(360, 168);
            chkLiveFromStart.AutoSize = true;
            chkLiveFromStart.FlatStyle = FlatStyle.Flat;
            chkLiveFromStart.FlatAppearance.BorderColor = ColorBorder;
            panelBody.Controls.Add(chkLiveFromStart);

            chkNoPlaylist = new CheckBox();
            chkNoPlaylist.Text = "Ignorar playlists (--no-playlist)";
            chkNoPlaylist.Location = new Point(360, 192);
            chkNoPlaylist.AutoSize = true;
            chkNoPlaylist.FlatStyle = FlatStyle.Flat;
            chkNoPlaylist.FlatAppearance.BorderColor = ColorBorder;
            chkNoPlaylist.Checked = true;
            panelBody.Controls.Add(chkNoPlaylist);

            chkEmbedSubs = new CheckBox();
            chkEmbedSubs.Text = "Embutir legendas (--embed-subs)";
            chkEmbedSubs.Location = new Point(360, 216);
            chkEmbedSubs.AutoSize = true;
            chkEmbedSubs.FlatStyle = FlatStyle.Flat;
            chkEmbedSubs.FlatAppearance.BorderColor = ColorBorder;
            panelBody.Controls.Add(chkEmbedSubs);

            // Main Download Button
            btnDownload = new Button();
            btnDownload.Text = "Iniciar Download";
            btnDownload.Location = new Point(20, 285);
            btnDownload.Size = new Size(660, 42);
            btnDownload.FlatStyle = FlatStyle.Flat;
            btnDownload.FlatAppearance.BorderSize = 0;
            btnDownload.BackColor = ColorAccent;
            btnDownload.ForeColor = Color.White;
            btnDownload.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnDownload.Cursor = Cursors.Hand;
            btnDownload.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            btnDownload.Click += new EventHandler(BtnDownload_Click);
            btnDownload.MouseEnter += new EventHandler(BtnDownload_MouseEnter);
            btnDownload.MouseLeave += new EventHandler(BtnDownload_MouseLeave);
            panelBody.Controls.Add(btnDownload);

            // Progress labels
            lblStatus = new Label();
            lblStatus.Text = "Status: Pronto para iniciar.";
            lblStatus.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            lblStatus.Location = new Point(20, 340);
            lblStatus.Width = 500;
            lblStatus.AutoSize = true;
            panelBody.Controls.Add(lblStatus);

            lblPercent = new Label();
            lblPercent.Text = "0%";
            lblPercent.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblPercent.Location = new Point(640, 340);
            lblPercent.Width = 40;
            lblPercent.TextAlign = ContentAlignment.TopRight;
            lblPercent.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            panelBody.Controls.Add(lblPercent);

            // Custom Flat Progress Bar
            progressBar = new CustomProgressBar();
            progressBar.Location = new Point(20, 365);
            progressBar.Size = new Size(660, 18);
            progressBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panelBody.Controls.Add(progressBar);

            // Detailed status labels (Speed, Size, ETA)
            lblSpeed = new Label();
            lblSpeed.Text = "Velocidade: -";
            lblSpeed.ForeColor = ColorTextMuted;
            lblSpeed.Location = new Point(20, 390);
            lblSpeed.Size = new Size(200, 20);
            panelBody.Controls.Add(lblSpeed);

            lblSize = new Label();
            lblSize.Text = "Tamanho: -";
            lblSize.ForeColor = ColorTextMuted;
            lblSize.Location = new Point(230, 390);
            lblSize.Size = new Size(200, 20);
            panelBody.Controls.Add(lblSize);

            lblEta = new Label();
            lblEta.Text = "Restante: -";
            lblEta.ForeColor = ColorTextMuted;
            lblEta.Location = new Point(440, 390);
            lblEta.Size = new Size(240, 20);
            lblEta.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            panelBody.Controls.Add(lblEta);

            // Show logs toggle
            chkShowLogs = new CheckBox();
            chkShowLogs.Text = "Mostrar console de log do yt-dlp";
            chkShowLogs.Location = new Point(20, 420);
            chkShowLogs.AutoSize = true;
            chkShowLogs.FlatStyle = FlatStyle.Flat;
            chkShowLogs.FlatAppearance.BorderColor = ColorBorder;
            chkShowLogs.CheckedChanged += new EventHandler(ChkShowLogs_CheckedChanged);
            panelBody.Controls.Add(chkShowLogs);

            // Restore defaults link label
            LinkLabel lnkRestore = new LinkLabel();
            lnkRestore.Text = "Restaurar Padrões";
            lnkRestore.Font = new Font("Segoe UI", 9.5F);
            lnkRestore.Location = new Point(560, 420);
            lnkRestore.AutoSize = true;
            lnkRestore.LinkColor = ColorAccent;
            lnkRestore.ActiveLinkColor = ColorAccentHover;
            lnkRestore.VisitedLinkColor = ColorAccent;
            lnkRestore.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lnkRestore.Click += new EventHandler(LnkRestore_Click);
            panelBody.Controls.Add(lnkRestore);

            // ================= LOG CONSOLE PANEL =================
            panelLogs = new Panel();
            panelLogs.Dock = DockStyle.Fill;
            panelLogs.Height = 0; // Collapsed by default
            panelLogs.Padding = new Padding(20, 0, 20, 20);
            panelLogs.Visible = false;
            mainLayout.Controls.Add(panelLogs, 0, 2);

            txtLog = new TextBox();
            txtLog.Multiline = true;
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.BackColor = Color.FromArgb(10, 15, 30); // Very dark navy-black for code
            txtLog.ForeColor = Color.FromArgb(16, 185, 129); // Emerald green output text
            txtLog.Font = new Font("Consolas", 9F);
            txtLog.Dock = DockStyle.Fill;
            txtLog.BorderStyle = BorderStyle.FixedSingle;
            panelLogs.Controls.Add(txtLog);
        }

        private void SetDefaultPaths()
        {
            // Default destination: User's Downloads folder
            string userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string downloadsPath = Path.Combine(userPath, "Downloads");
            if (Directory.Exists(downloadsPath))
            {
                txtDestFolder.Text = downloadsPath;
            }
            else
            {
                txtDestFolder.Text = AppDomain.CurrentDomain.BaseDirectory;
            }
        }

        private void BtnBrowseDest_Click(object sender, EventArgs e)
        {
            FolderPicker picker = new FolderPicker();
            picker.Title = "Selecione a pasta onde o download será salvo:";
            picker.InputPath = txtDestFolder.Text;
            if (picker.ShowDialog(this.Handle))
            {
                txtDestFolder.Text = picker.ResultPath;
            }
        }

        private void ChkUseCookies_CheckedChanged(object sender, EventArgs e)
        {
            txtCookiesPath.Enabled = chkUseCookies.Checked;
            btnBrowseCookies.Enabled = chkUseCookies.Checked;
        }

        private void BtnBrowseCookies_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Selecione o arquivo cookies.txt";
                ofd.Filter = "Arquivos de Texto (*.txt)|*.txt|Todos os Arquivos (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtCookiesPath.Text = ofd.FileName;
                }
            }
        }

        private void ChkShowLogs_CheckedChanged(object sender, EventArgs e)
        {
            if (chkShowLogs.Checked)
            {
                panelLogs.Visible = true;
                panelLogs.Height = 200;
                this.Height = 760;
            }
            else
            {
                panelLogs.Visible = false;
                panelLogs.Height = 0;
                this.Height = 560;
            }
        }

        private void BtnDownload_MouseEnter(object sender, EventArgs e)
        {
            if (!isDownloading)
            {
                btnDownload.BackColor = ColorAccentHover;
            }
        }

        private void BtnDownload_MouseLeave(object sender, EventArgs e)
        {
            if (!isDownloading)
            {
                btnDownload.BackColor = ColorAccent;
            }
        }

        private void BtnDownload_Click(object sender, EventArgs e)
        {
            if (isDownloading)
            {
                CancelDownload();
            }
            else
            {
                StartDownload();
            }
        }

        private void StartDownload()
        {
            string url = txtUrl.Text.Trim();
            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show("Por favor, insira uma URL válida da mídia online.", "URL Necessária", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string ytdlpPath = Path.Combine(appDir, "yt-dlp.exe");

            if (!File.Exists(ytdlpPath))
            {
                MessageBox.Show("Não foi possível encontrar o arquivo 'yt-dlp.exe' no diretório do aplicativo.\n" +
                                "Certifique-se de que ele está na mesma pasta que este executável.", "yt-dlp.exe Não Encontrado", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Ensure destination folder exists
            string destDir = txtDestFolder.Text.Trim();
            if (string.IsNullOrEmpty(destDir))
            {
                destDir = appDir;
            }
            try
            {
                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao criar a pasta de destino:\n" + ex.Message, "Erro de Diretório", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Build Arguments
            StringBuilder args = new StringBuilder();

            // Always add newline for clean progress parsing, and ffmpeg path
            args.Append("--newline ");
            args.Append("--ffmpeg-location \".\" ");

            // Quality / Format Options
            // "Melhor Qualidade (Padrão)", "1080p (Full HD)", "720p (HD)", "480p (SD)", "Apenas Áudio (MP3)"
            switch (cmbQuality.SelectedIndex)
            {
                case 1: // 1080p
                    args.Append("-f \"bestvideo[height<=1080]+bestaudio/best[height<=1080]\" --merge-output-format mkv ");
                    break;
                case 2: // 720p
                    args.Append("-f \"bestvideo[height<=720]+bestaudio/best[height<=720]\" --merge-output-format mkv ");
                    break;
                case 3: // 480p
                    args.Append("-f \"bestvideo[height<=480]+bestaudio/best[height<=480]\" --merge-output-format mkv ");
                    break;
                case 4: // Audio Only
                    args.Append("-x --audio-format mp3 ");
                    break;
                default: // Best
                    args.Append("-f \"bv*+ba/b\" --merge-output-format mkv ");
                    break;
            }

            // Advanced options
            if (chkLiveFromStart.Checked)
            {
                args.Append("--live-from-start ");
                // Lives are indeterminate, use marquee progress bar style by default
                progressBar.Style = CustomProgressBarStyle.Marquee;
            }
            else
            {
                progressBar.Style = CustomProgressBarStyle.Blocks;
            }

            if (chkNoPlaylist.Checked)
            {
                args.Append("--no-playlist ");
            }

            if (chkEmbedSubs.Checked)
            {
                args.Append("--embed-subs ");
            }

            if (chkUseCookies.Checked && !string.IsNullOrEmpty(txtCookiesPath.Text))
            {
                args.Append(string.Format("--cookies \"{0}\" ", txtCookiesPath.Text.Trim()));
            }

            // Output format template
            string outTemplate = Path.Combine(destDir, "%(title)s.%(ext)s");
            args.Append(string.Format("-o \"{0}\" ", outTemplate));

            // Add the URL at the end
            args.Append(string.Format("\"{0}\"", url));

            // UI Updates for starting state
            isDownloading = true;
            btnDownload.Text = "Cancelar Download";
            btnDownload.BackColor = Color.FromArgb(185, 28, 28); // Darker red for cancel
            ToggleInputs(false);
            
            txtLog.Clear();
            AppendLogText("[INICIANDO DOWNLOAD]\r\nComando: yt-dlp " + args.ToString() + "\r\n\r\n");
            
            lblStatus.Text = "Status: Iniciando yt-dlp...";
            lblPercent.Text = "0%";
            lblSpeed.Text = "Velocidade: -";
            lblSize.Text = "Tamanho: -";
            lblEta.Text = "Restante: -";
            progressBar.Value = 0;

            // Start Process
            try
            {
                currentProcess = new Process();
                currentProcess.StartInfo.FileName = ytdlpPath;
                currentProcess.StartInfo.Arguments = args.ToString();
                currentProcess.StartInfo.WorkingDirectory = appDir;
                currentProcess.StartInfo.UseShellExecute = false;
                currentProcess.StartInfo.RedirectStandardOutput = true;
                currentProcess.StartInfo.RedirectStandardError = true;
                currentProcess.StartInfo.CreateNoWindow = true;
                currentProcess.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                currentProcess.StartInfo.StandardErrorEncoding = Encoding.UTF8;

                currentProcess.OutputDataReceived += new DataReceivedEventHandler(Process_OutputDataReceived);
                currentProcess.ErrorDataReceived += new DataReceivedEventHandler(Process_ErrorDataReceived);
                currentProcess.EnableRaisingEvents = true;
                currentProcess.Exited += new EventHandler(Process_Exited);

                currentProcess.Start();
                currentProcess.BeginOutputReadLine();
                currentProcess.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao iniciar o processo:\n" + ex.Message, "Erro de Execução", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ResetUI(false, "Erro ao iniciar o processo.");
            }
        }

        private void CancelDownload()
        {
            if (currentProcess != null && !currentProcess.HasExited)
            {
                if (MessageBox.Show("Tem certeza que deseja cancelar o download?", "Confirmar Cancelamento", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    try
                    {
                        lblStatus.Text = "Status: Cancelando...";
                        
                        // Kill process tree recursively using taskkill on Windows
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "taskkill",
                            Arguments = "/T /F /PID " + currentProcess.Id,
                            CreateNoWindow = true,
                            UseShellExecute = false
                        });

                        ResetUI(false, "Download cancelado pelo usuário.");
                    }
                    catch (Exception ex)
                    {
                        AppendLogText("\r\nErro ao interromper processo: " + ex.Message);
                    }
                }
            }
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                AppendLogText(e.Data + "\r\n");
                ParseProgressLine(e.Data);
            }
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                AppendLogText("[ERRO] " + e.Data + "\r\n");
            }
        }

        private void Process_Exited(object sender, EventArgs e)
        {
            // The process has exited, invoke on main UI thread
            this.BeginInvoke(new Action(() =>
            {
                int exitCode = 0;
                try
                {
                    if (currentProcess != null)
                    {
                        exitCode = currentProcess.ExitCode;
                    }
                }
                catch { }

                if (isDownloading) // If we didn't reset already due to cancel
                {
                    if (exitCode == 0)
                    {
                        progressBar.Style = CustomProgressBarStyle.Blocks;
                        progressBar.Value = 100;
                        lblPercent.Text = "100%";
                        ResetUI(true, "Download concluído com sucesso!");
                    }
                    else
                    {
                        ResetUI(false, "Processo finalizado com erro (Código: " + exitCode + ").");
                        MessageBox.Show("Ocorreu um erro durante o download. Verifique o console de log para mais detalhes.", "Erro no Download", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }));
        }

        private void ParseProgressLine(string line)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<string>(ParseProgressLine), line);
                return;
            }

            try
            {
                line = line.Trim();
                if (string.IsNullOrEmpty(line)) return;

                // Handle stdout updates
                if (line.StartsWith("[download]"))
                {
                    // Check for video destination message
                    if (line.Contains("Destination:"))
                    {
                        lblStatus.Text = "Status: Baixando vídeo...";
                        return;
                    }

                    // Check for standard download progress line
                    // Example: [download]   2.3% of  12.34MiB at  1.23MiB/s ETA 00:08
                    // Example: [download] 100.0% of  12.34MiB in 00:10
                    Match pctMatch = Regex.Match(line, @"(\d+(?:\.\d+)?)%");
                    if (pctMatch.Success)
                    {
                        float pct = float.Parse(pctMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                        progressBar.Style = CustomProgressBarStyle.Blocks;
                        progressBar.Value = (int)pct;
                        lblPercent.Text = ((int)pct).ToString() + "%";
                        lblStatus.Text = "Status: Baixando arquivo...";
                    }

                    // Extract Size
                    Match sizeMatch = Regex.Match(line, @"of\s+([~\d.]+[a-zA-Z]+)");
                    if (sizeMatch.Success)
                    {
                        lblSize.Text = "Tamanho: " + sizeMatch.Groups[1].Value;
                    }
                    else
                    {
                        // Check if downloading live stream and shows downloaded amount
                        // Example: [download] 12.34MiB at 1.23MiB/s
                        Match downloadedMatch = Regex.Match(line, @"\[download\]\s+([\d.]+[a-zA-Z]+)\s+at");
                        if (downloadedMatch.Success)
                        {
                            lblSize.Text = "Baixado: " + downloadedMatch.Groups[1].Value;
                            progressBar.Style = CustomProgressBarStyle.Marquee;
                            lblPercent.Text = "-";
                        }
                    }

                    // Extract Speed
                    Match speedMatch = Regex.Match(line, @"at\s+([\d.]+[a-zA-Z]+/s|Unknown\s+speed|Unknown)");
                    if (speedMatch.Success)
                    {
                        lblSpeed.Text = "Velocidade: " + speedMatch.Groups[1].Value;
                    }

                    // Extract ETA or time
                    Match etaMatch = Regex.Match(line, @"ETA\s+([\d:]+|Unknown)");
                    if (etaMatch.Success)
                    {
                        lblEta.Text = "Restante: " + etaMatch.Groups[1].Value;
                    }
                    else
                    {
                        // Check for elapsed time in parentheses (lives)
                        Match timeMatch = Regex.Match(line, @"\(([\d:]+)\)");
                        if (timeMatch.Success)
                        {
                            lblEta.Text = "Decorrido: " + timeMatch.Groups[1].Value;
                        }
                    }
                }
                else if (line.StartsWith("[ffmpeg]"))
                {
                    progressBar.Style = CustomProgressBarStyle.Marquee;
                    lblStatus.Text = "Status: Processando/Mesclando formatos de áudio/vídeo...";
                    lblPercent.Text = "-";
                }
                else if (line.StartsWith("[ExtractAudio]"))
                {
                    progressBar.Style = CustomProgressBarStyle.Marquee;
                    lblStatus.Text = "Status: Extraindo e convertendo áudio...";
                    lblPercent.Text = "-";
                }
            }
            catch { }
        }

        private void AppendLogText(string text)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<string>(AppendLogText), text);
                return;
            }

            txtLog.AppendText(text);
            
            // Limit log text to prevent memory issues
            if (txtLog.TextLength > 100000)
            {
                txtLog.Text = txtLog.Text.Substring(txtLog.TextLength - 50000);
            }
        }

        private void ToggleInputs(bool enabled)
        {
            txtUrl.Enabled = enabled;
            txtDestFolder.Enabled = enabled;
            btnBrowseDest.Enabled = enabled;
            cmbQuality.Enabled = enabled;
            chkLiveFromStart.Enabled = enabled;
            chkNoPlaylist.Enabled = enabled;
            chkEmbedSubs.Enabled = enabled;
            chkUseCookies.Enabled = enabled;
            txtCookiesPath.Enabled = enabled && chkUseCookies.Checked;
            btnBrowseCookies.Enabled = enabled && chkUseCookies.Checked;
        }

        private void ResetUI(bool success, string statusMessage)
        {
            isDownloading = false;
            btnDownload.Text = "Iniciar Download";
            btnDownload.BackColor = ColorAccent;
            ToggleInputs(true);
            lblStatus.Text = "Status: " + statusMessage;
            if (!success && progressBar.Value < 100)
            {
                progressBar.Style = CustomProgressBarStyle.Blocks;
            }
            currentProcess = null;
        }

        private string GetConfigFilePath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.txt");
        }

        private void LoadConfig()
        {
            string configPath = GetConfigFilePath();
            if (!File.Exists(configPath))
            {
                SetDefaultPaths();
                return;
            }

            try
            {
                string[] lines = File.ReadAllLines(configPath);
                foreach (string line in lines)
                {
                    if (string.IsNullOrEmpty(line) || !line.Contains("=")) continue;
                    int idx = line.IndexOf('=');
                    string key = line.Substring(0, idx).Trim();
                    string val = line.Substring(idx + 1).Trim();

                    switch (key)
                    {
                        case "DestFolder":
                            txtDestFolder.Text = val;
                            break;
                        case "Quality":
                            int q;
                            if (int.TryParse(val, out q) && q >= 0 && q < cmbQuality.Items.Count)
                                cmbQuality.SelectedIndex = q;
                            break;
                        case "LiveFromStart":
                            bool lfs;
                            if (bool.TryParse(val, out lfs))
                                chkLiveFromStart.Checked = lfs;
                            break;
                        case "NoPlaylist":
                            bool np;
                            if (bool.TryParse(val, out np))
                                chkNoPlaylist.Checked = np;
                            break;
                        case "EmbedSubs":
                            bool es;
                            if (bool.TryParse(val, out es))
                                chkEmbedSubs.Checked = es;
                            break;
                        case "UseCookies":
                            bool uc;
                            if (bool.TryParse(val, out uc))
                                chkUseCookies.Checked = uc;
                            break;
                        case "CookiesPath":
                            txtCookiesPath.Text = val;
                            break;
                        case "ShowLogs":
                            bool sl;
                            if (bool.TryParse(val, out sl))
                                chkShowLogs.Checked = sl;
                            break;
                    }
                }
            }
            catch { }
        }

        private void SaveConfig()
        {
            try
            {
                string configPath = GetConfigFilePath();
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("DestFolder=" + txtDestFolder.Text);
                sb.AppendLine("Quality=" + cmbQuality.SelectedIndex);
                sb.AppendLine("LiveFromStart=" + chkLiveFromStart.Checked);
                sb.AppendLine("NoPlaylist=" + chkNoPlaylist.Checked);
                sb.AppendLine("EmbedSubs=" + chkEmbedSubs.Checked);
                sb.AppendLine("UseCookies=" + chkUseCookies.Checked);
                sb.AppendLine("CookiesPath=" + txtCookiesPath.Text);
                sb.AppendLine("ShowLogs=" + chkShowLogs.Checked);
                File.WriteAllText(configPath, sb.ToString());
            }
            catch { }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveConfig();
        }

        private void LnkRestore_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Tem certeza que deseja restaurar as configurações originais?", "Restaurar Padrões", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                // Reset fields
                SetDefaultPaths();
                cmbQuality.SelectedIndex = 0;
                chkLiveFromStart.Checked = false;
                chkNoPlaylist.Checked = true;
                chkEmbedSubs.Checked = false;
                chkUseCookies.Checked = false;
                txtCookiesPath.Text = "";
                txtCookiesPath.Enabled = false;
                btnBrowseCookies.Enabled = false;
                chkShowLogs.Checked = false;

                // Delete config file if it exists
                try
                {
                    string configPath = GetConfigFilePath();
                    if (File.Exists(configPath))
                    {
                        File.Delete(configPath);
                    }
                }
                catch { }

                MessageBox.Show("Configurações restauradas com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }

    public class FolderPicker
    {
        public string InputPath { get; set; }
        public string ResultPath { get; set; }
        public string Title { get; set; }

        public bool ShowDialog(IntPtr owner)
        {
            IFileDialog dialog = (IFileDialog)new FileOpenDialog();
            try
            {
                dialog.SetOptions(FOS.FOS_PICKFOLDERS | FOS.FOS_FORCEFILESYSTEM);
                if (!string.IsNullOrEmpty(Title))
                {
                    dialog.SetTitle(Title);
                }
                if (!string.IsNullOrEmpty(InputPath) && System.IO.Directory.Exists(InputPath))
                {
                    IShellItem item;
                    Guid guid = new Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"); // IID_IShellItem
                    SHCreateItemFromParsingName(InputPath, IntPtr.Zero, ref guid, out item);
                    if (item != null)
                    {
                        dialog.SetFolder(item);
                    }
                }

                int hr = dialog.Show(owner);
                if (hr == 0) // S_OK
                {
                    IShellItem result;
                    dialog.GetResult(out result);
                    if (result != null)
                    {
                        string path;
                        result.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out path);
                        ResultPath = path;
                        return true;
                    }
                }
                return false;
            }
            finally
            {
                Marshal.ReleaseComObject(dialog);
            }
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void SHCreateItemFromParsingName(
            [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            IntPtr pbc,
            ref Guid riid,
            [MarshalAs(UnmanagedType.Interface)] out IShellItem ppv);

        [ComImport]
        [Guid("42f85136-db7e-439c-85f1-e4075d135fc8")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileDialog
        {
            [PreserveSig] int Show(IntPtr parent);
            void SetFileTypes(); // Placeholder
            void SetFileTypeIndex(); // Placeholder
            void GetFileTypeIndex(); // Placeholder
            void Advise(); // Placeholder
            void Unadvise(); // Placeholder
            void SetOptions(FOS fos);
            void GetOptions(out FOS pfos);
            void SetDefaultFolder(IShellItem psi);
            void SetFolder(IShellItem psi);
            void GetFolder(out IShellItem ppsi);
            void GetCurrentSelection(out IShellItem ppsi);
            void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
            void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
            void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
            void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
            void GetResult(out IShellItem ppsi);
        }

        [ComImport]
        [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler(); // Placeholder
            void GetParent(); // Placeholder
            void GetDisplayName(SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
            void GetAttributes(); // Placeholder
            void Compare(); // Placeholder
        }

        [ComImport]
        [Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")]
        private class FileOpenDialog { }

        private enum FOS : uint
        {
            FOS_FORCEFILESYSTEM = 0x40,
            FOS_PICKFOLDERS = 0x20,
        }

        private enum SIGDN : uint
        {
            SIGDN_FILESYSPATH = 0x80058000,
        }
    }
}
