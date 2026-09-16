using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace AnkiImporter.Companion;

public sealed class SetupWizard : Form
{
    private readonly AnkiConnectClient _anki;
    private readonly PairingClient _pairing;
    private readonly Uri _serverUri;

    private readonly Label _title = new();
    private readonly Label _message = new();
    private readonly Label _status = new();
    private readonly Button _primary = new();
    private readonly Button _secondary = new();
    private readonly ProgressBar _progress = new();

    private int _step;

    public SetupWizard(AnkiConnectClient anki, PairingClient pairing, Uri serverUri)
    {
        _anki = anki;
        _pairing = pairing;
        _serverUri = serverUri;

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
        _message.Text = "Este assistente vai preparar a integração com o Anki Desktop.\n\nVocê não precisa configurar servidor, token ou portas.";
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
                await PairAsync();
                break;
            case 2:
                Close();
                break;
        }
    }

    private async Task CheckAnkiAsync()
    {
        SetBusy(true);
        _title.Text = "Verificando o Anki";
        _message.Text = "Estamos procurando o Anki Desktop e o AnkiConnect.";
        _status.Text = "Verificando...";

        try
        {
            var version = await _anki.InvokeAsync<int>("version");
            _status.Text = $"✓ Anki Desktop encontrado\n✓ AnkiConnect conectado (API {version})";
            _message.Text = "Tudo certo no Anki. Agora vamos conectar este computador ao Anki Importer no ChatGPT.";
            _step = 1;
            _primary.Text = "Conectar";
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

    private async Task PairAsync()
    {
        SetBusy(true);
        _title.Text = "Conectando ao ChatGPT";
        _message.Text = "Será exibido um código curto. Informe esse código ao Anki Importer no ChatGPT.\n\nO restante da configuração será feito automaticamente.";
        _status.Text = "Gerando código de conexão...";

        try
        {
            var config = await _pairing.PairAsync(
                _serverUri,
                code => BeginInvoke(() =>
                {
                    _status.Text = $"Código de conexão:   {code}\n\nAguardando confirmação no ChatGPT...";
                    SetBusy(true, keepPrimaryDisabled: true);
                }));

            await CompanionConfigStore.SaveAsync(config);

            _title.Text = "Tudo pronto";
            _message.Text = "Este computador está conectado ao Anki Importer. A partir de agora, basta usar o app no ChatGPT e anexar seu arquivo de vocabulário.";
            _status.Text = "✓ Anki conectado\n✓ ChatGPT conectado\n✓ Configuração salva com segurança";
            _step = 2;
            _primary.Text = "Finalizar";
            _secondary.Visible = false;
        }
        catch (Exception ex)
        {
            _status.Text = "Não foi possível concluir a conexão.";
            _message.Text = $"Verifique sua conexão com a internet e tente novamente.\n\nDetalhe: {ex.Message}";
            _step = 1;
            _primary.Text = "Tentar novamente";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy, bool keepPrimaryDisabled = false)
    {
        _progress.Visible = busy;
        _primary.Enabled = !busy && !keepPrimaryDisabled;
        _secondary.Enabled = !busy;
        UseWaitCursor = busy;
    }
}
