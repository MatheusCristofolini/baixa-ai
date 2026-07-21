# Baixa AI - Downloader de Mídias

Interface gráfica (GUI) leve, limpa e moderna para o utilitário de terminal `yt-dlp`. O aplicativo foi desenvolvido em C# nativo e Windows Forms, focado em facilidade de uso, privacidade e desempenho.

![Baixa AI](app_icon.ico)

---

## 📥 Como Baixar o Instalador Pronto

Se você quer apenas instalar e usar o programa imediatamente em qualquer computador (sem precisar compilar o código fonte):

1. Vá para a seção de **[Releases (Versões)](https://github.com/MatheusCristofolini/baixa-ai/releases)** do projeto.
2. Baixe o arquivo executável **`Setup_BaixaAI.exe`** anexado na versão mais recente.
3. Você também pode usar este link direto que sempre baixa a versão mais nova:
   👉 **[Baixar Versão Estável (Setup_BaixaAI.exe)](https://github.com/MatheusCristofolini/baixa-ai/releases/latest/download/Setup_BaixaAI.exe)**

---

## 🚀 Recursos Principais

- **Visual Moderno (Tema Escuro)**: Interface inspirada em designs premium, com transições suaves e tipografia Segoe UI.
- **Seletor de Pastas Nativo**: Integração com a API do Windows Explorer para seleção intuitiva do diretório de download.
- **Opções de Qualidade**: ComboBox para download fácil em Melhor Qualidade (MKV), 1080p, 720p, 480p ou extração direta de áudio em MP3.
- **Opções Avançadas para Lives**:
  - Download desde o início de transmissões ao vivo em andamento (`--live-from-start`).
  - Opção para embutir legendas diretamente no vídeo (`--embed-subs`).
  - Suporte para uso de arquivos de cookies (`cookies.txt`) para acessar transmissões restritas.
- **Progresso em Tempo Real**: Indicadores de porcentagem, velocidade de download, tamanho do arquivo e tempo estimado restante (ETA).
- **Persistência de Configurações**: Salva automaticamente suas últimas opções utilizadas (exceto URL) em um arquivo `config.txt` local, restaurando-as ao abrir o programa.
- **Restaurar Padrões**: Link de atalho rápido para redefinir as preferências do sistema aos valores de fábrica com apenas um clique.
- **Console de Logs Embutido**: Área expansível para visualizar a saída bruta da linha de comando do downloader.
- **Cancelamento Seguro**: Encerra o processo ativo e todas as suas dependências em execução com segurança.

---

## 🛠️ Como Compilar o Código Fonte

Para compilar o código fonte e gerar o executável e o instalador localmente, você não precisa instalar nenhuma ferramenta externa complexa. O Windows já possui o compilador C# nativo integrado no .NET Framework.

1. Abra um terminal do **PowerShell** no diretório do projeto.
2. Execute o script de compilação:
   ```powershell
   .\build.ps1
   ```
3. O script irá:
   - Compilar `BaixaAI.cs` gerando o executável `BaixaAI.exe`.
   - Localizar o compilador do Inno Setup (instalável via `winget install JRSoftware.InnoSetup`).
   - Gerar o instalador standalone compactado **`Setup_BaixaAI.exe`** no diretório raiz do projeto.

---

## 📦 Como Instalar e Usar

1. Execute o arquivo **`Setup_BaixaAI.exe`** gerado na pasta do projeto.
2. Siga as instruções do assistente (totalmente em português) e marque a opção para criar um atalho na Área de Trabalho se desejar.
3. Abra o **Baixa AI** a partir do atalho criado.
4. Cole o link do vídeo, áudio ou live stream desejado no campo correspondente.
5. Ajuste a qualidade e a pasta onde deseja salvar a mídia.
6. Clique em **Iniciar Download** e acompanhe o progresso pela tela.

> [!NOTE]
> Os binários do `ffmpeg.exe`, `ffprobe.exe` e `yt-dlp.exe` são essenciais e são empacotados automaticamente dentro do instalador. No repositório Git, esses executáveis estão listados no `.gitignore` devido ao limite de tamanho do GitHub (arquivos maiores que 100MB) e boas práticas de versionamento.
