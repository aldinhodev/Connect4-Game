using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class TcpServerLig4 : MonoBehaviour
{
    public int porta = 7777;
    private TcpListener listener;
    private TcpClient clienteConectado;
    private NetworkStream stream;
    private Thread threadAceitar;
    private Thread threadEscuta;

    public Lig4Manager lig4Manager;

    void Start()
    {
        if (lig4Manager == null) Debug.LogError("Arraste o Lig4Manager no Inspector do TcpServerLig4.");
        threadAceitar = new Thread(StartListening);
        threadAceitar.IsBackground = true;
        threadAceitar.Start();
    }

    void StartListening()
    {
        try
        {
            listener = new TcpListener(IPAddress.Any, porta);
            listener.Start();
            Debug.Log("Servidor TCP aguardando conexão na porta " + porta);

            clienteConectado = listener.AcceptTcpClient();
            stream = clienteConectado.GetStream();
            Debug.Log("Cliente conectado ao servidor.");

            threadEscuta = new Thread(EscutarCliente);
            threadEscuta.IsBackground = true;
            threadEscuta.Start();
        }
        catch (Exception ex)
        {
            Debug.LogError("Erro servidor: " + ex);
        }
    }

    void EscutarCliente()
    {
        byte[] buffer = new byte[1024];
        var sb = new StringBuilder();
        while (true)
        {
            try
            {
                int bytes = stream.Read(buffer, 0, buffer.Length);
                if (bytes == 0) { Debug.Log("Cliente desconectou."); break; }
                string msg = Encoding.UTF8.GetString(buffer, 0, bytes);
                Debug.Log("Servidor recebeu bruto: " + msg);

                sb.Append(msg);
                // processa mensagens terminadas em '\n'
                string all = sb.ToString();
                int idx;
                while ((idx = all.IndexOf('\n')) >= 0)
                {
                    string line = all.Substring(0, idx).Trim();
                    all = all.Substring(idx + 1);
                    var captured = line;
                    UnityMainThreadDispatcher.Instance()?.Enqueue(() =>
                    {
                        // Nesse projeto: o Lig4Manager espera receber apenas a coluna (ex: "3")
                        // ou mensagens especiais como "WIN|1"
                        lig4Manager.ProcessarMensagemRecebida(captured);
                    });
                }
                sb.Clear();
                sb.Append(all);
            }
            catch (Exception ex)
            {
                Debug.LogError("Erro leitura servidor: " + ex);
                break;
            }
        }
    }

    // Chamado pelo Lig4Manager quando o servidor (local) precisa enviar a jogada ao cliente
    public void EnviarJogada(int coluna)
    {
        SendToClient($"{coluna}\n");
    }

    // Opcional: enviar notificação explícita de vitória
    public void EnviarVitoria(int jogador)
    {
        SendToClient($"WIN|{jogador}\n");
    }

    public void SendToClient(string mensagem)
    {
        try
        {
            if (stream == null) { Debug.LogWarning("Stream nulo (cliente não conectado)."); return; }
            byte[] data = Encoding.UTF8.GetBytes(mensagem);
            stream.Write(data, 0, data.Length);
            stream.Flush();
            Debug.Log("Servidor enviou: " + mensagem.Trim());
        }
        catch (Exception ex)
        {
            Debug.LogError("Erro envio servidor: " + ex);
        }
    }

    private void OnApplicationQuit()
    {
        try { stream?.Close(); clienteConectado?.Close(); listener?.Stop(); }
        catch { }
    }
}

