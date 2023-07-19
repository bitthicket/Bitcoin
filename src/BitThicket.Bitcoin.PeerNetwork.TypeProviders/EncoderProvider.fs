namespace BitThicket.Bitcoin.PeerNetwork.TypeProviders

open System
open System.Reflection
open FSharp.Core.CompilerServices
open FSharp.Quotations
open FSharp.Reflection
open ProviderImplementation.ProvidedTypes


[<TypeProvider>]
type EncoderProvider (config : TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces(config, addDefaultProbingLocation=true)

    let ns = this.GetType().Namespace
    let assembly = Assembly.GetExecutingAssembly()

    let provider = ProvidedTypeDefinition(assembly, ns, "MessageEncoder", Some typeof<obj>)

    do provider.DefineStaticParameters(
        [ProvidedStaticParameter("msgTypeName", typeof<string>)],
        (fun typeName paramValues ->
            match paramValues with
            | [| :? string as msgTypeName |] ->
                let provided = ProvidedTypeDefinition(assembly, ns, typeName, Some typeof<obj>)
                provided
            | _ -> failwith "Invalid static parameters"
        ))

    do this.AddNamespace(ns, [provider])

[<assembly:TypeProviderAssembly>]
do ()