using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class TcpClientLig4 : MonoBehaviour
{
    public string ipServidor = "10.56.2.144"; // IP do servidor
    public int porta = 7777;
    private TcpClient cliente;
    private NetworkStream stream;
    private Thread threadReceber;
    private volatile bool rodando = false;

    public Lig4Manager lig4Manager; // Arraste no Inspector

    void Start()
    {
        threadReceber = new Thread(ConectarServidor);
        threadReceber.IsBackground = true;
        rodando = true;
        threadReceber.Start();
    }

    void ConectarServidor()
    {
        try
        {
            cliente = new TcpClient();
            cliente.Connect(ipServidor, porta);
            stream = cliente.GetStream();
            Debug.Log("Conectado ao servidor!");

            byte[] buffer = new byte[1024];
            while (rodando && cliente != null && cliente.Connected)
            {
                try
                {
                    if (!stream.DataAvailable)
                    {
                        Thread.Sleep(10);
                        continue;
                    }

                    int bytesLidos = stream.Read(buffer, 0, buffer.Length);
                    if (bytesLidos > 0)
                    {
                        string mensagem = Encoding.UTF8.GetString(buffer, 0, bytesLidos);
                        Debug.Log("Recebido do servidor: " + mensagem);

                        // Enqueue para thread principal usando a instância lig4Manager
                        UnityMainThreadDispatcher.Instance().Enqueue(() =>
                        {
                            if (lig4Manager != null)
                                lig4Manager.ProcessarMensagemRecebida(mensagem);
                        });
                    }
                }
                catch (Exception ex)
                {
                    Debug.Log("Erro leitura cliente: " + ex.Message);
                    break;
                }
            }
        }
        catch (SocketException ex)
        {
            Debug.Log("Erro cliente (conexão): " + ex.Message);
        }
        finally
        {
            Fechar();
        }
    }

    public void EnviarJogada(int coluna)
    {
        try
        {
            if (stream != null && stream.CanWrite)
            {
                byte[] dados = Encoding.UTF8.GetBytes(coluna.ToString());
                stream.Write(dados, 0, dados.Length);
                stream.Flush();
                Debug.Log("Enviado para servidor: " + coluna);
            }
        }
        catch (Exception ex)
        {
            Debug.Log("Erro ao enviar (cliente): " + ex.Message);
        }
    }

    public void EnviarMensagem(string mensagem)
    {
        try
        {
            if (stream != null && stream.CanWrite)
            {
                byte[] dados = Encoding.UTF8.GetBytes(mensagem);
                stream.Write(dados, 0, dados.Length);
                stream.Flush();
                Debug.Log("Enviado para servidor: " + mensagem);
            }
        }
        catch (Exception ex)
        {
            Debug.Log("Erro ao enviar mensagem (cliente): " + ex.Message);
        }
    }

    void Fechar()
    {
        rodando = false;
        try { stream?.Close(); } catch { }
        try { cliente?.Close(); } catch { }
    }

    private void OnApplicationQuit()
    {
        Fechar();
        try { threadReceber?.Interrupt(); } catch { }
    }
}
