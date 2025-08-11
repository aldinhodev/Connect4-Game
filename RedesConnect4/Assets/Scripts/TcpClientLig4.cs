using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class TcpClientLig4 : MonoBehaviour
{
    public string ipServidor = "127.0.0.1";
    public int porta = 7777;
    private TcpClient client;
    private NetworkStream stream;
    private Thread threadEscuta;

    public Lig4Manager lig4Manager;

    void Start()
    {
        if (lig4Manager == null) Debug.LogError("Arraste o Lig4Manager no Inspector do TcpClientLig4.");
        ConnectToServer();
    }

    public void ConnectToServer()
    {
        threadEscuta = new Thread(() =>
        {
            try
            {
                client = new TcpClient();
                client.Connect(ipServidor, porta);
                stream = client.GetStream();
                Debug.Log("Cliente conectado ao servidor " + ipServidor + ":" + porta);

                byte[] buffer = new byte[1024];
                var sb = new System.Text.StringBuilder();
                while (true)
                {
                    int bytes = stream.Read(buffer, 0, buffer.Length);
                    if (bytes == 0) { Debug.Log("Servidor desconectou."); break; }
                    string msg = Encoding.UTF8.GetString(buffer, 0, bytes);
                    Debug.Log("Cliente recebeu bruto: " + msg);

                    sb.Append(msg);
                    string all = sb.ToString();
                    int idx;
                    while ((idx = all.IndexOf('\n')) >= 0)
                    {
                        string line = all.Substring(0, idx).Trim();
                        all = all.Substring(idx + 1);
                        var captured = line;
                        UnityMainThreadDispatcher.Instance()?.Enqueue(() =>
                        {
                            lig4Manager.ProcessarMensagemRecebida(captured);
                        });
                    }
                    sb.Clear();
                    sb.Append(all);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Erro cliente: " + ex);
            }
        });
        threadEscuta.IsBackground = true;
        threadEscuta.Start();
    }

          

    // Chamado pelo Lig4Manager quando o cliente (local) joga e precisa enviar ao servidor
    public void EnviarJogada(int coluna)
    {
        SendToServer($"{coluna}\n");
    }

    // Opcional: notificacao explícita de vitoria (normalmente o servidor envia)
    public void EnviarVitoria(int jogador)
    {
        SendToServer($"WIN|{jogador}\n");
    }

    public void SendToServer(string mensagem)
    {
        try
        {
            if (stream == null) { Debug.LogWarning("Stream nulo, não conectado."); return; }
            byte[] data = Encoding.UTF8.GetBytes(mensagem);
            stream.Write(data, 0, data.Length);
            stream.Flush();
            Debug.Log("Cliente enviou: " + mensagem.Trim());
        }
        catch (Exception ex)
        {
            Debug.LogError("Erro envio cliente: " + ex);
        }
    }

    private void OnApplicationQuit()
    {
        try { stream?.Close(); client?.Close(); }
        catch { }
    }
}
