(*Copyright 2016 Andrew M. Olney

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.*)
// Load Fable.Core and bindings to JS global objects
#r "../node_modules/fable-core/Fable.Core.dll"
#load "../node_modules/fable-import-react/Fable.Import.React.fs"
#load "../node_modules/fable-import-react/Fable.Helpers.React.fs"
#load "../node_modules/fable-react-toolbox/Fable.Helpers.ReactToolbox.fs"
#load "../node_modules/fable-elmish/elmish.fs"
#load "../node_modules/fable-import-d3/Fable.Import.D3.fs"
#load "graph.fsx"

open System
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.Node
//open Fable.Import.Electron
open Fable.Helpers.ReactToolbox
open Fable.Helpers.React.Props
open Elmish
open Graph

let [<Literal>] ENTER_KEY = 13.

module R = Fable.Helpers.React
module RT = Fable.Helpers.ReactToolbox

type RCom = React.ComponentClass<obj>

type IGinger =
    abstract init: unit -> unit

let Ginger = importMember<unit->IGinger>("../app/js/ginger.js")
let ginger = Ginger()
//let ReactFauxDOM = importAll<obj> "react-faux-dom/lib/ReactFauxDOM"
//let MyD3 = importAll<obj> "d3"

type IMary =
    abstract ``process``: text:string*options:obj*callback:Func<obj,unit> -> unit 
    abstract phonemes: words:string array*local:string*voice:string*callback:Func<obj,unit> -> unit 
    abstract voices: callback:Func<obj,unit> -> unit
    abstract locales: callback:Func<obj,unit> -> unit
    abstract inputTypes: callback:Func<obj,string array> -> unit
    abstract outputTypes: callback:Func<obj,string array> -> unit
    abstract audioFormats: callback:Func<obj,string array> -> unit

//let Mary = importMember<string*int->IMary> "marytts"
//http://localhost:59125/process?INPUT_TYPE=TEXT&AUDIO=WAVE_FILE&OUTPUT_TYPE=AUDIO&LOCALE=en-US&INPUT_TEXT=%22Hi%20there%22
//let mary = Mary("localhost",59125)

let Mary : obj = importMember "marytts"
let mary : IMary = createNew Mary ("localhost", 59125) |> unbox
let WebView = importDefault<RCom> "react-electron-webview" 


let inline (!!) x = createObj x
let inline (=>) x y = x ==> y


// Local storage interface
module S =
    let private STORAGE_KEY = "braintrust-electron"
    let load<'T> (): 'T option =
        Browser.localStorage.getItem(STORAGE_KEY)
        |> unbox 
        |> Core.Option.map (JS.JSON.parse >> unbox<'T>)

    let save<'T> (model: 'T) =
        Browser.localStorage.setItem(STORAGE_KEY, JS.JSON.stringify model)

// MODEL
type Model = {
    count:int
    tabIndex : int
    isChecked : bool
    info : string
    url : string
    MorphValue : float
    force: D3.Layout.Force<Link,Node>
    }


type Msg =
    | Increment
    | Decrement
    | TabIndex of int
    | Check of bool
    | Info of string
    | Url of string
    | Navigate
    | NavigateForward
    | NavigateBackward
    | Refresh
    | Close
    | UpdateNavigationUrl of string
    | MorphValueChange
    | AddNode
    | Speak

let emptyModel =  
    {    
        count = 0; 
        tabIndex = 0; 
        isChecked = true; 
        url = "http://www.google.com"; 
        info = "something here"; 
        MorphValue = 0.0 
        force  = D3.Layout.Globals.force() :?> D3.Layout.Force<Link,Node> 
    }

//Initialize app and return initial model
let init = function
    | Some savedModel -> savedModel
    | _ -> emptyModel


// UPDATE
/// Uses Fable's Emit to call JavaScript directly
[<Emit("(new Audio($0)).play();")>]
let sound(file:string) : unit = failwith "never"
let update (msg:Msg) (model:Model)  =
    let webView = Browser.document.getElementById("webview")
    match msg with
    | Increment ->
        { model with count = model.count + 1 }
    | Decrement ->
        { model with count = model.count - 1 }
    | TabIndex(index) -> 
        { model with tabIndex = index}
    | Check(check) ->
        { model with isChecked = check}
    | Info(i) ->
        { model with info = i}
    | Url(i) ->
        { model with url = i}
    | Navigate ->
        webView?loadURL( model.url ) |> ignore
        model
    | NavigateForward ->
        webView?goForward() |> ignore
        model
    | NavigateBackward ->
        webView?goBack() |> ignore
        model
    | Refresh ->
        webView?reload() |> ignore
        model
    | Close ->
        webView?stop() |> ignore
        model
    | UpdateNavigationUrl(i) ->
        { model with url = i}
    | MorphValueChange ->
        //send something to ginger here
        model
    | AddNode ->
        //it is important to mutate existing nodes. if we create new ones, e.g. with Array.map, existing links will break
        let x,y = 10.0, 10.0 //totally arbitrary
        let node = {index = None; x = Some(x); y = Some(y); px = None; py = None; ``fixed``=None; weight = None}
        model.force.nodes()?push(node) |> ignore
        restart( model.force ) |> ignore
        model
    | Speak ->
        mary.``process``(
            "Hello World", 
            createObj[ "base64" ==> true], 
            fun audio -> sound( audio |> unbox<string> )
            //Browser.console.log(audio)  
        )
        model

// VIEW
let internal onEnter msg dispatch =
    function 
    | (ev:React.KeyboardEvent) when ev.keyCode = ENTER_KEY ->
        ev.preventDefault() 
        dispatch msg
    | _ -> ()
    |> OnKeyDown

let internal onClick msg dispatch =
    OnClick <| fun _ -> msg |> dispatch 

(*let viewLeftPaneOrg model dispatch =

    R.div [ Style [ GridArea "1 / 1 / 2 / 1";  ] ] //row start / col start / row end / col end    
        [
            RT.appBar [ AppBarProps.LeftIcon "grade" ] []
            RT.tabs [ Index model.tabIndex; TabsProps.OnChange ( TabIndex >> dispatch ) ] [
                RT.tab [ Label "Buttons" ] [
                    R.section [] [
                        RT.button [ Icon "help"; Label "Help"; ButtonProps.Primary true; Raised true ] []
                        RT.button [ Icon "home"; Label "Home"; Raised true ] []
                        RT.button [ Icon "rowing"; Floating true ] []
                        RT.iconButton [ Icon "power_settings_new"; IconButtonProps.Primary true ] []
                    ]
                ]
                RT.tab [ Label "Inputs" ] [
                    R.section [] [
                        RT.input [ Type "text"; Label "Information"; InputProps.Value model.info; InputProps.OnChange ( Info >> dispatch ) ] []
                        RT.checkbox [ Label "Check me"; Checked model.isChecked; CheckboxProps.OnChange ( Check >> dispatch ) ] []
                        RT.switch [ Label "Switch me"; Checked model.isChecked; SwitchProps.OnChange(  Check >> dispatch ) ] []
                    ]
                ]
                RT.tab [ Label "List" ] [
                    RT.list [] [
                        RT.listSubHeader [ Caption "Listing" ] []
                        RT.listDivider [] []
                        RT.listItem [ Caption "Item 1"; Legend "Keeps it simple" ] []
                        RT.listDivider [] []
                        RT.listItem [ Caption "Item 2"; Legend "Turns it up a notch"; RightIcon <| Case2("star") ] []
                        RT.listDivider [] []
                        RT.listItem [ Caption "Item 3"; Legend "Turns it up a notch 2"; RightIcon <| Case2("star") ] []
                        RT.listDivider [] []
                        RT.listItem [ Caption "Item 4"; Legend "Turns it up a notch 3"; RightIcon <| Case2("star") ] []
                    ]
                ]
            ]
        ]
let viewRightPaneOrg model dispatch = 
    let onClick msg =
        OnClick <| fun _ -> msg |> dispatch 

    R.div [ Style [ GridArea "1 / 2 / 1 / 2"  ] ] [
    //R.div [ Style [  Flex (U2.Case2 "1 1 auto")  ] ] [
        RT.button [ Icon "add"; Label "Add"; Raised true; onClick Increment ] []
        R.div [] [ unbox (string model.count) ]
        R.div [] [ unbox (string model.tabIndex) ]
        R.div [] [ unbox (string model.isChecked) ]
        R.div [] [ unbox (string model.info) ]
        R.p [] [ unbox "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum."]
        RT.button [ Icon "remove"; Label "Remove"; Raised true; onClick Decrement ] []
    ]
*)

let viewLeftPane model dispatch =
    R.div [ Style [ GridArea "1 / 1 / 3 / 1"; CSSProp.Width (U2.Case2 "100%"); CSSProp.Height (U2.Case2 "100%");  ] ] [
        R.div [ ] [ //Style [ Display "Flex"; FlexDirection "Row"]] [ //trying to make all one row
            RT.iconButton [ Icon "arrow_back"; onClick NavigateBackward dispatch][]
            RT.iconButton [ Icon "arrow_forward"; onClick NavigateForward dispatch][]
            RT.iconButton [ Icon "refresh"; onClick Refresh dispatch][]
            RT.iconButton [ Icon "close"; onClick Close dispatch][]
            RT.input [ Type "text"; InputProps.Value model.url; InputProps.OnChange ( Url >> dispatch ); onEnter Navigate dispatch ] []               
        ]
        R.from WebView
            !![
                "src" => "http://www.google.com";
                "style" => [CSSProp.Height "100%" ];
                "id" => "webview";
        ][]
        
    ]

let viewRightPane model dispatch = 
    let onClick msg =
        OnClick <| fun _ -> msg |> dispatch 
    R.div [] [
        R.div [ Style [ GridArea "1 / 2 / 1 / 2"  ] ] [
            R.div [ Id "renderer" ] []
            R.p [] [ unbox "Alright. So the important thing to remember is that ..."]
            RT.iconMenu  [ Id "morph" ; IconMenuProps.Icon (U2.Case2 "more_vert"); IconMenuProps.Position "topLeft" ] [
                RT.menuItem [ MenuItemProps.Value "eyes"; MenuItemProps.Caption "Eyes"  ] [  ]
                RT.menuItem [ MenuItemProps.Value "expression"; MenuItemProps.Caption "Expression"  ] [  ]
                RT.menuItem [ MenuItemProps.Value "jawrange"; MenuItemProps.Caption "Jaw Height"  ] [  ]
                RT.menuItem [ MenuItemProps.Value "jawtwist"; MenuItemProps.Caption "Jaw Twist"  ] [  ]
                RT.menuItem [ MenuItemProps.Value "symmetry"; MenuItemProps.Caption "Symmetry"  ] [  ]
                RT.menuItem [ MenuItemProps.Value "lipcurl"; MenuItemProps.Caption "Lip Curl"  ] [  ]
                RT.menuItem [ MenuItemProps.Value "sex"; MenuItemProps.Caption "Face Structure"  ] [  ]
                RT.menuItem [ MenuItemProps.Value "width"; MenuItemProps.Caption "Jaw Width"  ] [  ]
                RT.menuItem [ MenuItemProps.Value "tongue"; MenuItemProps.Caption "Tongue"  ] [  ]
            ]
            //SliderProps.OnChange ( MorphValueChange >> dispatch ); //needs JS.Function?
            //SliderProps.Value model.MorphValue; //need to be able to change model
            RT.slider [ Id "range"; SliderProps.Editable true; SliderProps.Min 0.0; SliderProps.Max 1.0;   SliderProps.Step 0.01  ] []
        ]
        R.div [ Style [ GridArea "2 / 3 / 2 / 3"  ] ] [
            RT.button [ Label "Speak"; Raised true; onClick Speak ] []
            RT.button [ Label "Add Node"; Raised true; onClick AddNode ] []
            R.com<ForceDirectedGraph,_,_> 
                { new ForceDirectedGraphProps with
                    member __.force = model.force
                } []
        ]
    ]
let viewMain model dispatch =
    //R.div [ Style [ Display "flex"; FlexDirection "row";  CSSProp.Width (U2.Case2 "100%"); CSSProp.Height (U2.Case2 "100%");] ] [
    R.div [ Style [ Display "grid"; GridTemplateRows "30% 70%"; GridTemplateColumns "60% 40%"; CSSProp.Width (U2.Case2 "100%"); CSSProp.Height (U2.Case2 "100%"); ] ] [
        viewLeftPane model dispatch
        viewRightPane model dispatch
    ]


// App
let program = 
    //fable-elmish todomvc has a Program.mkProgram example, but what that buys you is currently unclear to me
    //also we imitate their storage here but they have no save to storage wired up, so this is not doing anything right now
    Program.mkSimple (S.load >> init) update
    |> Program.withConsoleTrace

type App() as this =
    inherit React.Component<obj, Model>()
    
    let safeState state =
        match unbox this.props with 
        | false -> this.state <- state
        | _ -> this.setState state

    let dispatch = program |> Program.run safeState

    member this.componentDidMount() =
        //take care of webView
        let webView = Browser.document.getElementById("webview")
        webView?addEventListener("did-start-loading", 
            fun ev -> UpdateNavigationUrl( "Loading..." ) |> dispatch ) |> ignore
        webView?addEventListener("did-stop-loading", 
            fun () -> UpdateNavigationUrl (unbox (webView?getURL()))  |> dispatch ) |> ignore
        //take care of ginger (TODO: react-three-renderer)
        ginger.init()

        this.props <- true

    member this.render() =
       viewMain this.state dispatch



ReactDom.render(
        R.com<App,_,_> () [],
        Browser.document.getElementById("app")
    ) |> ignore
