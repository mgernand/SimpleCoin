# SimpleCoin

To get started with a local three-node setup from a Windows PowerShell.

```powershell
cd SimpleCoin.Node
dotnet restore
dotnet build
start dotnet -Args run, --port=5000
start dotnet -Args run, --port=5001
start dotnet -Args run, --port=5002
```

## HTTP REST API

The REST API is demoed using the curl command. To have it available on Windows install
it using the Chocolatey package manager Windows: https://chocolatey.org/.

```powershell
choco install curl
choco upgrade url
start cmd
```

### Peer-to-Peer

##### Ping a node

```bash
curl http://localhost:5000/ping
```

##### Add a peer

```bash
curl -H "Content-type:application/json" --data "{'peer': 'localhost:5001'}" http://localhost:5000/addPeer
```

##### Query connected peers

```bash
curl http://localhost:5000/peers
``` 

##### Send a test message to connected nodes

```bash
curl http://localhost:5000/hello
```

### Blockchain


##### Get all blocks of the blockchain

```
curl http://localhost:5000/blocks
```

##### Create a new block

```
curl -H "Content-type:application/json" --data "{'data' : 'BLOCKDATA'}" http://localhost:5000/mineBlock
``` 
