# Akash Mass Deploy

Akash Mass Deploy is a console app for managing a large number of deployments on the
[Akash](https://akash.network/) network.

## Motivation

The tools available for deploying workloads to Akash have been a desktop app,
[Akashlytics Deploy](https://github.com/Akashlytics/akashlytics-deploy); a web app,
[Akash Deploy](https://github.com/spacepotahto/akash-deploy-ui); and the
[native command-line interface](https://github.com/ovrclk/docs/blob/master/general-commands.md).
These tools are ideal for single deployments but aren’t suited to orchestrating multiple deployments
across the network.

The Akash Mass Deploy tool was created to run a [Massive](https://joinmassive.com/) testnet on Akash
infrastructure and can be used to automate similar mass deployments.

## Summary

Akash Mass Deploy extends the command-line interface to add coordination functions by connecting to
an Akash-loaded Linux instance over SSH.

Deployments are created with the user-provided Akash wallet, certificate, and configuration files;
active deployments are maximized by closing any that become stale and maintaining an exclusion list
of providers that fail repeatedly for the same configuration.

## Main functions
 - **Invoking without arguments** - will create number of deployments specified in js file from "CREATE_DEPLOYMENTS" field with "DEFAULT_CORES" of cores per each deployment. It will
 - **closedead** - will close all unfinished/invalid instances and return money from them to the wallet + also will calculate amount of uakt that is currently being spent
 - **deposits** - will fill instances with funds, currently refill so they will equal 5akt
 - **manifests** - will update all machines with new yml deployment file, number of cores & name will be regenerated
 - **info** - will show additional info for all created machines
 
## Current limitations 

 - since it's experimental tool, it's currently not supports paging and
   has limits of 500 instances that can be controlled at once
 - password managment is currently done for "file" as supplying user
   password directly to bash or for "os" with prompt on first usage
 - currently almost all calls have up to 3 retries for bad cases such as overloaded node or bad networks, but there are still possibilities of failed deployments due of number of external factors   
 - it has been developed in monodevelop for ubuntu and has not been tested on windows
## Preparation

 - install akash on system
 - generate wallet
 - generate certificate for wallet
 - upload it to the network
## Short information about classes in source code

 - EnvVarsReplacer - is a class that evaluates all variables in bash(initially there were a lot of them, I've simplified so it just precaches them all for future uses)
 - Main part is Akash - it does ssh connection to localhost, group commands together and sends them, gets errors/results and print them
 - ClientSSH - is an addition class to manage ssh connection
 - Wallet - is the akash wallet - it has wallet names and function for getting current amount
 - Converters - various YML/JSON converters, akash price converter and random generator
Instance - is the main akash instance concept, here we create new deployment, select optimal bids, send manifest, add deposit, close - basically do everything with our deployment
 - InstanceList in current state - it basically iterates over all deployments that are deployed currently in order to close dead ones(and release the funds), deposit to the ones that are low on money and show statistics
It currently generates a lot of files for debugging.
Currently it just consumes the YML renames the name of the server to random value and does the submission + checks dead/deposit to the instances
 - InstanceList - is the class for queryies list of instances, checking their deployment, filling them with money
 - Program - is currently main class in that utility
 
