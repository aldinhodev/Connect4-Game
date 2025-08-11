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
    private Thread threadEscuta;
    private volatile bool rodando = false;

    public Lig4Manager lig4Manager; // Arraste no Inspector

    void Start()
    {
        threadEscuta = new Thread(OuvirCliente);
        threadEscuta.IsBackground = true;
        rodando = true;
        threadEscuta.Start();
    }

    void OuvirCliente()
    {
        try
        {
            listener = new TcpListener(IPAddress.Any, porta);
            listener.Start();
            Debug.Log("Servidor aguardando conexão...");

            clienteConectado = listener.AcceptTcpClient();
            Debug.Log("Cliente conectado!");

            stream = clienteConectado.GetStream();

            byte[] buffer = new byte[1024];
            while (rodando && clienteConectado != null && clienteConectado.Connected)
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
                        Debug.Log("Recebido do cliente: " + mensagem);

                        UnityMainThreadDispatcher.Instance().Enqueue(() =>
                        {
                            if (lig4Manager != null)
                                lig4Manager.ProcessarMensagemRecebida(mensagem);
                        });
                    }
                }
                catch (Exception ex)
                {
                    Debug.Log("Erro servidor (leitura): " + ex.Message);
                    break;
                }
            }
        }
        catch (SocketException ex)
        {
            Debug.Log("Erro servidor (aceitar): " + ex.Message);
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
                Debug.Log("Enviado para cliente: " + coluna);
            }
        }
        catch (Exception ex)
        {
            Debug.Log("Erro ao enviar (servidor): " + ex.Message);
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
                Debug.Log("Enviado para cliente: " + mensagem);
            }
        }
        catch (Exception ex)
        {
            Debug.Log("Erro ao enviar mensagem (servidor): " + ex.Message);
        }
    }

    void Fechar()
    {
        rodando = false;
        try { stream?.Close(); } catch { }
        try { clienteConectado?.Close(); } catch { }
        try { listener?.Stop(); } catch { }
    }

    private void OnApplicationQuit()
    {
        Fechar();
        try { threadEscuta?.Interrupt(); } catch { }
    }
}
