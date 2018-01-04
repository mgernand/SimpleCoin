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

##### Create the first block only with a coinbase transaction.

```
curl -H "Content-type:application/json" --data "{'data' : [{'txIns':[{'signature':'','txOutId':'','txOutIndex':1}],'txOuts':[{'address':'04bfcab8722991ae774db48f934ca79cfb7dd991229153b9f732ba5334aafcd8e7266e47076996b55a14bf9913ee3145ce0cfc1372ada8ada74bd287450313534a','amount':50}],'id':'f089e8113094fab66b511402ecce021d0c1f664a719b5df1652a24d532b2f749'}]}' http://localhost:5000/mineBlock
``` 

The private-key: ```19f128debc1b9122da0635954488b208b829879cf13b3d6cac5d1260c0fd967c```