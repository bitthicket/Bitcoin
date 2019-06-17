module BitThicket.Bitcoin.Daemon.Util

open Microsoft.FSharp.Quotations.Patterns

// https://stackoverflow.com/a/26621814/3279
let getModuleType = function
| PropertyGet (_, propertyInfo, _) -> propertyInfo.DeclaringType
| _ -> failwith "Expression is no property"

let rec private _moduleType = getModuleType <@ _moduleType @>