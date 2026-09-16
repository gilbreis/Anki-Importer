using System.Drawing;
using System.Windows.Forms;

namespace AnkiImporter.Companion;

public sealed class SetupWizard : Form
{
    private readonly AnkiConnectClient _anki;

    private readonly Label _title = new();
    private readonly Label _message = new();
    private readonly Label _status = new();
    private readonly Button _primary = new();
    private readonly Button _secondary = new();
    private readonly ProgressBar _progress = new();

    private int _step;

    public SetupWizard(AnkiConnectClient anki)
    {
        _anki = anki;

        Text = "Anki Importer";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Width = 620;
        Height = 390;
        Font = new Font("Segoe UI", 10F);

        BuildUi();
        ShowWelcome();
    }

    private void BuildUi()
    {
        _title.SetBounds(36, 28, 540, 44);
        _title.Font = new Font("Segoe UI Semibold", 20F);

        _message.SetBounds(38, 88, 530, 90);
        _message.Font = new Font("Segoe UI", 11F);

        _status.SetBounds(38, 190, 530, 60);
        _status.Font = new Font("Segoe UI Semibold", 10.5F);

        _progress.SetBounds(38, 260, 530, 8);
        _progress.Style = ProgressBarStyle.Marquee;
        _progress.Visible = false;

        _secondary.SetBounds(350, 295, 100, 36);
        _secondary.Text = "Cancelar";
        _secondary.Click += (_, _) => Close();

        _primary.SetBounds(462, 295, 106, 36);
        _primary.Click += async (_, _) => await PrimaryClickAsync();

        Controls.AddRange([_title, _message, _status, _progress, _secondary, _primary]);
    }

    private void ShowWelcome()
    {
        _step = 0;
        _title.Text = "Bem-vindo ao Anki Importer";
        _message.Text = "Este assistente vai preparar a integração com o Anki Desktop.\n\nVocê não precisa configurar servidor, token, porta ou código de conexão.";
        _status.Text = "";
        _primary.Text = "Avançar";
        _secondary.Visible = true;
    }

    private async Task PrimaryClickAsync()
    {
        switch (_step)
        {
            case 0:
                await CheckAnkiAsync();
                break;
            case 1:
                Close();
                break;
        }
    }

    private async Task CheckAnkiAsync()
    {
        SetBusy(true);
        _title.Text = "Verificando o Anki";
        _message.Text = "Estamos verificando o Anki Desktop e o AnkiConnect.";
        _status.Text = "Verificando...";

        try
        {
            var version = await _anki.InvokeAsync<int>("version");
            _title.Text = "Instalação concluída";
            _status.Text = $"✓ Anki Desktop encontrado\n✓ AnkiConnect conectado (API {version})";
            _message.Text = "Tudo certo neste computador.\n\nAgora volte ao ChatGPT e clique em “Conectar este computador”. O restante será automático.";
            _step = 1;
            _primary.Text = "Finalizar";
            _secondary.Visible = false;
        }
        catch
        {
            _status.Text = "AnkiConnect não foi encontrado.";
            _message.Text = "Abra o Anki Desktop e confirme que o complemento AnkiConnect está instalado. Depois clique em Tentar novamente.";
            _primary.Text = "Tentar novamente";
            _step = 0;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        _progress.Visible = busy;
        _primary.Enabled = !busy;
        _secondary.Enabled = !busy;
        UseWaitCursor = busy;
    }
}
