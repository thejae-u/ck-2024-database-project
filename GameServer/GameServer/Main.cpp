#pragma comment(lib, "ws2_32.lib")
#include "TCPServer.h"

const int PORT = 56000;

int main()
{
    TCPServer server(PORT);
    if (server.start()) {
        server.run();
    }

    return 0;
}
