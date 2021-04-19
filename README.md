# Synced Network

## About

This projeft was created for two main purposes:

- To learn networking in c#
- To create a networking solution that can be used for future projects

## Learning networking

As I previously mentioned, this project was helpful in learning networking. I learned a lot about c# tcp connections, sending data, recieving data, and handling data. I later used this project to create an online game using the 3d game engine, Unity.

## Solution usage

To use this project, simply import the DLL's located under `/Builds/1.0.0/` for your client and server accordingly. You can find a "Demo Client" script in `/Client/`, and a "Demo Server" script located in `/Server/`. Messages are sent using custom Message objects, which is stored in a Dictionary.

Important note: Be sure that the messages dictionary is identical on the client and server. You must include all messages wether they are being sent from the server or client.

## Info

Created on: `1/17/21`
Skills learned: C# networking using TCP
Project difficulty: 7/10
