open FSharp.Data
type FedoraServer = JsonProvider<"https://geoip.fedoraproject.org/city">

[<EntryPoint>]
let main argv = 
    let date = FedoraServer.GetSample()
    let city = date.City
    printfn "%A" city
    0
