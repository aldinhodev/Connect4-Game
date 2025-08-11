using UnityEngine;
using UnityEngine.UI;

public class Lig4Manager : MonoBehaviour
{
    public GameObject pecaJogador1;  // Amarelo
    public GameObject pecaJogador2;  // Vermelho
    public Transform boardPanel;
    public Text textoStatus;

    public TcpServerLig4 tcpServer;  // Para servidor
    public TcpClientLig4 tcpClient;  // Para cliente

    private int[,] grade = new int[6, 7];
    private int jogadorAtual = 1;   // Quem tem o turno agora (1 ou 2)
    private bool jogoAtivo = true;

    private int jogadorLocal = 1;  // 1 = Amarelo (Servidor), 2 = Vermelho (Cliente)

    void Start()
    {
        // Define o jogadorLocal conforme a cor configurada no PlayerInfo
        jogadorLocal = (PlayerInfo.CorDoJogador == "AMARELO") ? 1 : 2;

        jogadorAtual = 1; // Amarelo sempre começa

        ResetarTabuleiro();
        ConfigurarBotoes();
        AtualizarTextoStatus();
    }

    void ConfigurarBotoes()
    {
        Button[] botoes = boardPanel.GetComponentsInChildren<Button>();
        for (int i = 0; i < botoes.Length; i++)
        {
            int index = i;
            botoes[i].onClick.RemoveAllListeners();
            botoes[i].onClick.AddListener(() => OnSlotClick(index));
            botoes[i].interactable = true;
        }
    }

    void OnSlotClick(int index)
    {
        if (!jogoAtivo) return;

        int coluna = index % 7;

        // Só permite jogar se for o turno do jogador local
        if (jogadorAtual != jogadorLocal)
        {
            Debug.Log("Não é seu turno.");
            return;
        }

        if (grade[0, coluna] != 0)
        {
            Debug.Log("Coluna cheia.");
            return;
        }

        // Executa a jogada local e envia para o adversário
        FazerJogadaLocal(coluna);
    }

    // Faz a jogada no tabuleiro
    public void FazerJogadaLocal(int coluna, bool enviarParaRede = true, int jogador = -1)
    {
        if (jogador == -1) jogador = jogadorAtual; // Se não passar, usa o turno atual

        for (int linha = 5; linha >= 0; linha--)
        {
            if (grade[linha, coluna] == 0)
            {
                grade[linha, coluna] = jogador;

                int slotIndex = linha * 7 + coluna;
                Transform slotTransform = boardPanel.GetChild(slotIndex);

                // Instancia a peça correta conforme o jogador que fez a jogada
                GameObject peca = Instantiate(
                    jogador == 1 ? pecaJogador1 : pecaJogador2,
                    slotTransform
                );
                peca.transform.localPosition = Vector3.zero;
                peca.transform.localRotation = Quaternion.identity;
                peca.transform.localScale = Vector3.one;
                peca.tag = "Peca";

                Button btn = slotTransform.GetComponent<Button>();
                if (btn != null)
                    btn.interactable = false;

                // Verifica se ganhou
                if (VerificarVitoria(linha, coluna, jogador))
                {
                    textoStatus.text = $"Jogador {ObterNomeJogador(jogador)} venceu!";
                    jogoAtivo = false;
                    DesabilitarBotoes();
                    return;
                }

                // Verifica empate
                if (VerificarEmpate())
                {
                    textoStatus.text = "Empate!";
                    jogoAtivo = false;
                    DesabilitarBotoes();
                    return;
                }

                // Se for jogada local, envia para o outro
                if (enviarParaRede)
                {
                    EnviarJogadaRede(coluna);
                }

                // Troca turno *depois* de processar tudo
                jogadorAtual = 3 - jogador;

                AtualizarTextoStatus();

                return;
            }
        }

        Debug.Log("Coluna cheia.");
    }

    // Recebe jogada remota
    public void FazerJogadaRemota(int coluna)
    {
        if (!jogoAtivo) return;

        int jogadorRemoto = jogadorLocal == 1 ? 2 : 1;
        Debug.Log($"FazerJogadaRemota chamada! coluna = {coluna} jogadorRemoto = {jogadorRemoto}");

        // Aqui avisa que é jogada recebida, então não envia de novo
        FazerJogadaLocal(coluna, false, jogadorRemoto);
    }

    // Envia jogada para o outro jogador via rede
    void EnviarJogadaRede(int coluna)
    {
        if (jogadorLocal == 1)
        {
            if (tcpServer != null)
            {
                tcpServer.EnviarJogada(coluna);
            }
        }
        else
        {
            if (tcpClient != null)
            {
                tcpClient.EnviarJogada(coluna);
            }
        }
    }

    // --- NOVO: processa mensagens vindas da rede (chamado por TcpClient/TcpServer via dispatcher)
    public void ProcessarMensagemRecebida(string mensagem)
    {
        if (string.IsNullOrEmpty(mensagem)) return;

        // Pode vir múltiplas mensagens concatenadas. Separate por nova linha e por espaços.
        string[] tokens = mensagem.Split(new char[] { '\n', '\r', ';', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (var t in tokens)
        {
            string s = t.Trim();
            if (int.TryParse(s, out int coluna))
            {
                // Segurança: validar intervalo
                if (coluna >= 0 && coluna < 7)
                {
                    FazerJogadaRemota(coluna);
                }
                else
                {
                    Debug.LogWarning("Coluna recebida fora do intervalo: " + coluna);
                }
            }
            else
            {
                Debug.LogWarning("Mensagem de rede inválida (não é número): " + s);
            }
        }
    }

    void AtualizarTextoStatus()
    {
        if (!jogoAtivo) return;

        textoStatus.text = $"Turno do {ObterNomeJogador(jogadorAtual)}";
    }

    void DesabilitarBotoes()
    {
        Button[] botoes = boardPanel.GetComponentsInChildren<Button>();
        foreach (var btn in botoes)
            btn.interactable = false;
    }

    bool VerificarEmpate()
    {
        for (int linha = 0; linha < 6; linha++)
            for (int col = 0; col < 7; col++)
                if (grade[linha, col] == 0)
                    return false;
        return true;
    }

    bool VerificarVitoria(int linha, int coluna, int jogador)
    {
        int[][] direcoes = new int[][]
        {
            new int[] {0, 1},   // horizontal
            new int[] {1, 0},   // vertical
            new int[] {1, 1},   // diagonal \
            new int[] {1, -1}   // diagonal /
        };

        foreach (var dir in direcoes)
        {
            int count = 1;
            count += ContarNaDirecao(linha, coluna, dir[0], dir[1], jogador);
            count += ContarNaDirecao(linha, coluna, -dir[0], -dir[1], jogador);
            if (count >= 4) return true;
        }
        return false;
    }

    int ContarNaDirecao(int linha, int coluna, int dLinha, int dColuna, int jogador)
    {
        int cont = 0;
        int l = linha + dLinha;
        int c = coluna + dColuna;

        while (l >= 0 && l < 6 && c >= 0 && c < 7 && grade[l, c] == jogador)
        {
            cont++;
            l += dLinha;
            c += dColuna;
        }
        return cont;
    }

    public void ResetarTabuleiro()
    {
        // Limpa a matriz
        for (int linha = 0; linha < 6; linha++)
        {
            for (int col = 0; col < 7; col++)
            {
                grade[linha, col] = 0;
            }
        }

        // Remove as peças da tela
        foreach (Transform slot in boardPanel)
        {
            foreach (Transform child in slot)
            {
                Destroy(child.gameObject);
            }

            // Reabilita o botão
            Button btn = slot.GetComponent<Button>();
            if (btn != null)
                btn.interactable = true;
        }

        jogoAtivo = true;
        jogadorAtual = 1;
        AtualizarTextoStatus();
    }

    string ObterNomeJogador(int jogador)
    {
        return jogador == 1 ? "Amarelo" : "Vermelho";
    }
}
