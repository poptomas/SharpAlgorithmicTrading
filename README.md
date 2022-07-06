# Cryptocurrency Algorithmic Trading Bot

## Table of Contents
- [Cryptocurrency Algorithmic Trading Bot](#cryptocurrency-algorithmic-trading-bot)
  - [Table of Contents](#table-of-contents)
  - [Introduction](#introduction)
    - [Brief Description](#brief-description)
    - [Motivation](#motivation)
  - [Technology Used](#technology-used)
  - [Setup and Launch](#setup-and-launch)
  - [Hardware Requirements](#hardware-requirements)

## Introduction
### Brief Description

Cryptocurrency Algorithmic Trading Bot (CATB) is a console application written in C# which serves as a simulator for algorithmic trading with cryptocurrencies.
Currently supported cryptocurrency exchange is [Binance](https://www.binance.com/). Within the application there is no real money involved.

CATB supports various commands which are requested from the user via ```Console.ReadLine()```, as shown below:
```
----------------------------------------
Supported commands (case insensitive, without <>):
help - ................ - prints this help
deposit <value> - ..... - adds amount of cash to your account
withdraw - ............ - withdraw all your currently possessed cryptocurrencies and end the session
assets - .............. - shows the amount of your currently possessed cryptocurrencies including cash currency
transactions - ........ - shows your recently accomplished transactions
market - .............. - shows current cryptocurrency market prices from your watchlist
indicators - .......... - shows indicators concerning your cryptocurrency watchlist
add <symbol> - ........ - adds a cryptocurrency symbol to your watchlist
remove <symbol> - ..... - removes a cryptocurrency symbol from your watchlist
----------------------------------------
```
By the way, this is the expected output of ```help``` command.

Disclaimer: in case anyone uses results produced by the CATB for their own financial decisions, the author of the project holds no responsibility for potential losses.

### Motivation
The main motivation behind the simulator creation was to learn more about cryptocurrency in general
and whether such a volatile field can be somehow predicted. There are plenty of options which can
be considered, for instance, using deep neural networks ([RNN](https://stanford.edu/~shervine/teaching/cs-230/cheatsheet-recurrent-neural-networks) - Recurrent Neural Networks or [LSTM](https://stanford.edu/~shervine/teaching/cs-230/cheatsheet-recurrent-neural-networks#architecture) - Long Short Term Memory).
Nevertheless, an option which was chosen for this task was strictly algorithmic trading using technical indicators which
are fairly common in the field of "human cryptocurrency trading".

## Technology Used
- C#
    - Modern ```C#``` (```C# 10``` available with .NET 6) due global using directives
    - Test run was made using C# 10 in Visual Studio 2022 (v17.2)

- External dependencies
    - None

## Setup and Launch
- The only thing needed is to open AlgorithmicTrading.sln solution file, within VS 2022 choose whether to run Debug/Release
- Compile and run

## Hardware Requirements
- For an end user, there are no special hardware requirements
- Note that although the program should be prepared for a connection loss, 
to make use of the functionality provided, ensure you are connected to the internet. 
In case you do not have an internet connection at the beginning of the program (after filling in the cryptocurrency watchlist), 
the program ends with an error message; otherwise if you lose connection during the program run, 
the program keeps running and keeps shouting the connection to the service was lost, 
so it does not receive any updates from the API service
- The application was compiled and run using a device with the CPU: AMD Ryzen 7 4800H, RAM 16 GB,
Windows 10 Home (64-bit) using Visual Studio 2022 (v17.2)