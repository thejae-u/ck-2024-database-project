#pragma once
#include <iostream>
#include <WinSock2.h>
#include <WS2tcpip.h>
#include <thread>
#include <vector>
#include "ClientHandler.h"


class TCPServer {
public:
    TCPServer(int port) : port(port), server_socket(INVALID_SOCKET) {}

    bool start() {
        WSADATA wsaData;
        int result = WSAStartup(MAKEWORD(2, 2), &wsaData);
        if (result != 0) {
            std::cerr << "WSAStartup failed with error: " << result << std::endl;
            return false;
        }

        server_socket = socket(AF_INET, SOCK_STREAM, 0);
        if (server_socket == INVALID_SOCKET) {
            std::cerr << "Could not create socket, error: " << WSAGetLastError() << std::endl;
            WSACleanup();
            return false;
        }

        sockaddr_in server_addr;
        server_addr.sin_family = AF_INET;
        server_addr.sin_addr.s_addr = INADDR_ANY;
        server_addr.sin_port = htons(port);

        if (bind(server_socket, (sockaddr*)&server_addr, sizeof(server_addr)) == SOCKET_ERROR) {
            std::cerr << "Bind failed with error: " << WSAGetLastError() << std::endl;
            closesocket(server_socket);
            WSACleanup();
            return false;
        }

        if (listen(server_socket, SOMAXCONN) == SOCKET_ERROR) {
            std::cerr << "Listen failed with error: " << WSAGetLastError() << std::endl;
            closesocket(server_socket);
            WSACleanup();
            return false;
        }

        std::cout << "Server listening on port " << port << std::endl;
        return true;
    }

    void run() {
        while (true) {
            sockaddr_in client_addr;
            int client_size = sizeof(client_addr);
            SOCKET client_socket = accept(server_socket, (sockaddr*)&client_addr, &client_size);
            if (client_socket == INVALID_SOCKET) {
                std::cerr << "Accept failed with error: " << WSAGetLastError() << std::endl;
                continue;
            }

            std::unique_ptr<std::thread> client_thread(new std::thread(ClientHandler::handleClient, client_socket));
            client_thread->detach();
            client_threads.push_back(std::move(client_thread));
        }
    }

    ~TCPServer() {
        if (server_socket != INVALID_SOCKET) {
            closesocket(server_socket);
        }
        WSACleanup();
    }

private:
    int port;
    SOCKET server_socket;
    std::vector<std::unique_ptr<std::thread>> client_threads;
};