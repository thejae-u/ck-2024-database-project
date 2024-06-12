#pragma once
#include <iostream>
#include <WinSock2.h>
#include <WS2tcpip.h>

#define BUFFER_SIZE 1024

class ClientHandler {
public:
    static void handleClient(SOCKET client_socket) {
        char buffer[BUFFER_SIZE];
        std::cout << "Connected by client: " << client_socket << std::endl;

        while (true) {
            std::memset(buffer, 0, BUFFER_SIZE);
            int bytes_received = recv(client_socket, buffer, BUFFER_SIZE - 1, 0);
            if (bytes_received <= 0) {
                std::cerr << "Error in recv() or connection closed by client: " << client_socket << std::endl;
                break;
            }

            std::cout << "Received from client " << client_socket << ": " << buffer << std::endl;

            int bytes_sent = send(client_socket, buffer, bytes_received, 0);
            if (bytes_sent == SOCKET_ERROR) {
                std::cerr << "Error in send() to client: " << client_socket << std::endl;
                break;
            }
        }

        closesocket(client_socket);
        std::cout << "Connection with client " << client_socket << " closed" << std::endl;
    }
};
