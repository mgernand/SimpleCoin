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
curl -H "Content-type:application/json" --data "{'peer': 'localhost:5001'}" http://localhost:5000/peers
```

##### Query connected peers

```bash
curl http://localhost:5000/peers
``` 

### Blockchain


##### Get all blocks of the blockchain

```bash
curl http://localhost:5000/blocks
```

##### Get a specific block

```bash
curl http://localhost:5000/blocks/{hash}
```


##### Mine a block

```
curl -X POST http://localhost:5000/mineBlock
``` 


### Wallet

##### Mine transaction

```bash
curl -H "Content-type: application/json" --data "{'address': '04bfcab8722991ae774db48f934ca79cfb7dd991229153b9f732ba5334aafcd8e7266e47076996b55a14bf9913ee3145ce0cfc1372ada8ada74bd287450313534b', 'amount' : 35}" http://localhost:5000/mineTransaction
```

The private-key: ```19f128debc1b9122da0635954488b208b829879cf13b3d6cac5d1260c0fd967c```

##### Send transaction

```bash
curl -H "Content-type: application/json" --data "{'address': '04bfcab8722991ae774db48f934ca79cfb7dd991229153b9f732ba5334aafcd8e7266e47076996b55a14bf9913ee3145ce0cfc1372ada8ada74bd287450313534b', 'amount' : 35}" http://localhost:5000/sendTransaction
```

##### Query transaction pool

```bash
curl http://localhost:5000/transaction_pool_
```

##### Get a specific transaction

```bash
curl http://localhost:5000/transactions/{id}
```

##### Get balance

```bash
curl http://localhost:5000/balance
```

##### Get balance of a specific address

```bash
curl http://localhost:5000/balance/{address}
```

##### Get wallet address

```bash
curl http://localhost:5000/address
```
