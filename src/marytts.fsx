
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

//we added this function to the node package
(*
		durations: function(text, options, callback) {
			// If options are not provided let's hope the second parameter is the callback function
			callback = typeof(options) === 'function' ? options : callback;
			options = typeof(options) === 'object' ? options : {};

			// Create the data to transmit
			var data = [];
			data['INPUT_TEXT'] = text;
			data['INPUT_TYPE'] = (!('inputType' in options) || !(options.inputType in InputTypes)) ? 'TEXT' : options.inputType.toUpperCase();
			data['OUTPUT_TYPE'] = (!('outputType' in options) || !(options.outputType in OutputTypes)) ? 'REALISED_DURATIONS' : options.outputType.toUpperCase();
			data['LOCALE'] = (!('locale' in options)) ? 'en_US' : options.locale;
//			data['VOICE'] = (!('voice' in options)) ? 'cmu-slt-hsmm' : options.voice;
			data['AUDIO'] = (!('audio' in options) || !(options.audio in AudioFormats)) ? 'WAVE_FILE' : options.audio.toUpperCase();

			if('voice' in options && options.voice.length > 0) data['VOICE'] = options.voice;
		
			request(
				{
					url: _url + 'process',
					method: 'POST',
					form: data,
					encoding: data['OUTPUT_TYPE']==='AUDIO' ? null : 'utf8'
				},
				function (error, response, body) {
					if(error) {
						console.error(error);
						return;
					}
					//console.log(response);
					if (response.statusCode == 200) {
					var lines = body.split('\n'),
						timings = [];
					for (var i = 1; i < lines.length; i++) {
						var line = lines[i];
						if(line.length > 0) {
							var split = line.split(' ');
							timings.push( {
								'time': split[0],
								'number': split[1],
								'phoneme': split[2]
							})
						}
					};
						callback(timings);
							
					} else {
						console.error(response.statusCode + ': ' + response.statusMessage);
					}
				}
			);
		},
        *)