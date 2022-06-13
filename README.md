# Akash Mass Deploy

**Akash Mass Deploy** is a console app for managing a large number of deployments on the
[Akash](https://akash.network/) network.

## Motivation

The tools available for deploying workloads to Akash, a desktop app,
[Akashlytics Deploy](https://github.com/Akashlytics/akashlytics-deploy); a web app,
[Akash Deploy](https://github.com/spacepotahto/akash-deploy-ui); and the
[native command-line interface](https://github.com/ovrclk/akash), are ideal for single deployments
but aren’t suited to orchestrating multiple deployments across the network.

The **Akash Mass Deploy** tool was developed to run a [Massive](https://joinmassive.com/) testnet on
Akash infrastructure and can be used to automate similar mass deployments.

## Summary

**Akash Mass Deploy** extends the command-line interface to add coordination functions by connecting
to an Akash-loaded Linux instance over SSH.

Deployments are created with the user-supplied Akash wallet, certificate, and configuration files;
active deployments are maximized by closing any that become stale and by maintaining an exclusion
list of providers that fail repeatedly for the deployment configuration.

## Configuring

1. [Install Akash](https://github.com/ovrclk/docs/blob/master/guides/cli.md#part-1-install-akash) on
   the target Linux instance.
2. [Create an Akash wallet](https://github.com/ovrclk/docs/blob/master/token/keplr.md).
3. [Create and publish a certificate](https://github.com/ovrclk/docs/blob/master/guides/cli.md#part-6-create-your-certificate)
   for the wallet.

## Usage

The deployment mode is determined by one of the following command-line arguments:

* `[none]`    – creates the number of deployments `CREATE_DEPLOYMENTS` with the number of cores per
                deployment `DEFAULT_CORES` given by the `config.js` file
* `manifests` – updates all active deployments with the current `deploy.yml` file
* `deposits`  – tops all active deployments up, to 5 AKT currently
* `cleanup`   – tears all nonfunctioning deployments down
* `info`      – returns the state of all deployments

## Limitations

**Akash Mass Deploy** is experimental at the moment and, in particular:

* Can control no more than 500 simultaneous deployments and doesn’t support paging
* Prompts for a password on first run, via dialog box if `AKASH_KEYRING_BACKEND` is set to (the
  default) `os` or via command line if set to `file`
* Retries most Akash commands up to 3x but still doesn’t account for all intermittent failure modes,
  like those involving connectivity problems
* Has been developed in MonoDevelop for Linux and hasn’t been tested in Windows

## C# class details

The following classes define important functionality:

* [`MainClass`](Main.cs#L9-L151)                 – is the app entry point
* [`EnvVarsReplacer`](EnvVarsReplacer.cs#L7-L41) – evaluates environment variables, caching for
                                                   future use any that are set
* [`ClientSSH`](ClientSSH.cs#L9-L94)             – manages the SSH connection
* [`Wallet`](Wallet.cs#L8-L85)                   – maintains Akash wallet state
* [`Instance`](Instance.cs#L8-L602)              – creates, updates, and tears deployments down
* [`InstanceList`](InstanceList.cs#L14-L263)     – batches deployment commands and output, refilling
                                                   and releasing funds as needed
* [`Converters`](Converters.cs#L10-L104)         – performs various data conversions

## License

Copyright 2022 Massive Computing, Inc.

This program is free software, excluding the brand features identified in the
[Exceptions](#exceptions) below: you can redistribute it and/or modify it under the terms of the GNU
Affero General Public License as published by the Free Software Foundation, either version 3 of the
License, or (at your option) any later version.

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
[GNU Affero General Public License](https://www.gnu.org/licenses/agpl-3.0.html) for more details.

## Exceptions

The Massive logos, trademarks, domain names, and other brand features used in this program cannot be
reused without permission and no license is granted thereto.
