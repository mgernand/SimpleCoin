# SimpleCoin


##### Add a peer

```
curl -H "Content-type:application/json" --data '{"peer" : "localhost:5001"}' http://localhost:5000/addPeer
```

#### Query connected peers

```
curl http://localhost:5000/peers
``` 

#### Send a test message to connected nodes

```
curl http://localhost:5000/hello
```

