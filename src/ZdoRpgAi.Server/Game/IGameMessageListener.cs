using ZdoRpgAi.Protocol.Rpc;

namespace ZdoRpgAi.Server.Game;

public interface IGameMessageListener {
    void OnClientConnected(IRpcChannel client);
    void OnClientDisconnected();
}
