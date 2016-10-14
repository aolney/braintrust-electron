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
//#load "../node_modules/fable-import-electron/Fable.Import.Electron.fs"
#load "../node_modules/fable-react-toolbox/Fable.Helpers.ReactToolbox.fs"
#load "../node_modules/fable-elmish/elmish.fs"

open System
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.Node
//open Fable.Import.Electron
open Fable.Helpers.ReactToolbox
open Fable.Helpers.React.Props
open Elmish

let [<Literal>] ENTER_KEY = 13.

module R = Fable.Helpers.React
module RT = Fable.Helpers.ReactToolbox

type RCom = React.ComponentClass<obj>
let WebView = importDefault<RCom> "react-electron-webview" 
let inline (!!) x = createObj x
let inline (=>) x y = x ==> y


// Local storage interface
module S =
    let private STORAGE_KEY = "fable-electron-elmish-react-reacttoolbox"
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

let emptyModel =  { count = 0; tabIndex = 0; isChecked = true; url = "http://www.google.com"; info = "something here" }

//Initialize app and return initial model
let init = function
    | Some savedModel -> savedModel
    | _ -> emptyModel


// UPDATE
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
                "style" => [CSSProp.Height "100%"];
                "id" => "webview";
        ][]
        
    ]
let viewLeftPaneOrg model dispatch =

    
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

let viewRightPane model dispatch = 
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



let viewMain model dispatch =
    //R.div [ Style [ Display "flex"; FlexDirection "row";  CSSProp.Width (U2.Case2 "100%"); CSSProp.Height (U2.Case2 "100%");] ] [
    R.div [ Style [ Display "grid"; GridTemplateRows "30% 70%"; GridTemplateColumns "40% 60%"; CSSProp.Width (U2.Case2 "100%"); CSSProp.Height (U2.Case2 "100%"); ] ] [
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
        let webView = Browser.document.getElementById("webview")
        webView?addEventListener("did-start-loading", 
            fun ev -> UpdateNavigationUrl( "Loading..." ) |> dispatch ) |> ignore
        webView?addEventListener("did-stop-loading", 
            fun () -> UpdateNavigationUrl (unbox (webView?getURL()))  |> dispatch ) |> ignore
        this.props <- true

    member this.render() =
       viewMain this.state dispatch



ReactDom.render(
        R.com<App,_,_> () [],
        Browser.document.getElementById("app")
    ) |> ignore
