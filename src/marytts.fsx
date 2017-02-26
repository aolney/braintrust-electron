
(*Copyright 2017 Andrew M. Olney

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.*)

#r "../node_modules/fable-core/Fable.Core.dll"
open System
open Fable.Core

/// Uses Fable's Emit to call JavaScript directly
[<Emit("(new Audio($0)).play();")>]
let sound(file:string) : unit = failwith "never"

type IMary =
    abstract ``process``: text:string*options:obj*callback:Func<obj,unit> -> unit 
    abstract durations: text:string*options:obj*callback:Func<obj array,unit> -> unit 
    abstract phonemes: words:string array*locale:string*voice:string*callback:Func<obj,unit> -> unit 
    abstract voices: callback:Func<obj,unit> -> unit
    abstract locales: callback:Func<obj,unit> -> unit
    abstract inputTypes: callback:Func<obj,string array> -> unit
    abstract outputTypes: callback:Func<obj,string array> -> unit
    abstract audioFormats: callback:Func<obj,string array> -> unit

//let Mary = importMember<string*int->IMary> "marytts"
//http://localhost:59125/process?INPUT_TYPE=TEXT&AUDIO=WAVE_FILE&OUTPUT_TYPE=AUDIO&LOCALE=en-US&INPUT_TEXT=%22Hi%20there%22
//let mary = Mary("localhost",59125)

type Duration =
    {
        phoneme : string
        number : int
        time : float
    }
(*"0"	PhonOh
"@"	PhonAah
"@U"	PhonAah
"A"	PhonAah
"AI"	PhonAah
"D"	PhonDST
"E"	PhonEe
"EI"	PhonEh
"I"	PhonI
"N"	PhonN
"O"	PhonOh 
"OI"	PhonOohQ
"S"	PhonDST
"T"	PhonDST
"U"	PhonW
"V"	PhonFV
"Z"	PhonDST
"_"	PhonBMP
"aU"	PhonAah
"b"	PhonBMP
"d"	PhonDST
"dZ"	PhonChJSh
"f"	PhonFV
"g"	PhonK
"h"	PhonK
"i"	PhonI
"j"	PhonI
"k"	PhonK
"l"	PhonN
"m"	PhonBMP
"n"	PhonN
"p"	PhonBMP
"r"	PhonR
"r="	PhonR
"s"	PhonDST
"t"	PhonDST
"tS"	PhonChJSh
"u"	PhonW
"v"	PhonFV
"w"	PhonW
"z"	PhonDST
"{"	PhonTh*)
///For sock puppet lipsync
let openPhonemes = Set.ofList["0"; "@"; "@U"; "A"; "AI"; "E"; "EI"; "I"; "O"; "OI"; "aU"; "i";"r=";]