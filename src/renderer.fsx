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
#load "../node_modules/fable-import-d3/Fable.Import.D3.fs"

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

type IGinger =
    abstract init: unit -> unit

let Ginger = importMember<unit->IGinger>("../app/js/ginger.js")
let ReactFauxDOM = importAll<obj> "react-faux-dom/lib/ReactFauxDOM"
let MyD3 = importAll<obj> "d3"
let WebView = importDefault<RCom> "react-electron-webview" 
let ginger = Ginger()

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
type Datum =
    {
    Date : System.DateTime
    Close : float
}

type Model = {
    count:int
    tabIndex : int
    isChecked : bool
    info : string
    url : string
    Data: Datum array
    }

let GetDataFromFile =
    let parseDate = D3.Time.Globals.format("%d-%b-%y").parse
    let tsv = Node.fs.readFileSync("app/data/data.tsv", "utf8")
    let data =
        tsv.Trim().Split('\n')
        |> Array.skip 1 //skip header
        |> Array.map( fun row ->
            let s = row.Split('\t')
            let date = parseDate( s.[0] )
            let close =  s.[1] |> float 
            {Date=date;Close=close})
    data

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

let emptyModel =  { count = 0; tabIndex = 0; isChecked = true; url = "http://www.google.com"; info = "something here"; Data = GetDataFromFile }

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
let ReactD3 (model:Model) =
    let marginTop,marginRight,marginBottom,marginLeft = 20,20,30,50
    let width = 960 - marginLeft  - marginRight
    let height = 500 - marginTop  - marginBottom

    //30-Apr-12
    let parseDate = D3.Time.Globals.format("%d-%b-%y").parse

    let x = 
        D3.Time.Globals.scale<float,float>()
            .range([|0.0; float width|])

    let y = 
        D3.Scale.Globals.linear()
            .range([|float height; 0.0|])

    let xAxis = 
        D3.Svg.Globals.axis()
            .scale(x)
            .orient("bottom")

    let yAxis = 
        D3.Svg.Globals.axis()
            .scale(y)
            .orient("left")

    let line2 = 
        D3.Svg.Globals.line<Datum>() 
            .x( System.Func<Datum,float,float>(fun d _ -> x.Invoke(d.Date) ) )
            .y( System.Func<Datum,float,float>(fun d _ -> y.Invoke(d.Close) ) )

    //this is a dynamic version of the typed line above
    //most of the below goes dynamic, especially for things like attr, which otherwise would need erasable types
    let line =
        D3.Svg.Globals.line() 
            ?x( fun d -> x$(d.Date ))
            ?y( fun d -> y$(d.Close ))

    let node  = ReactFauxDOM?createElement("svg") :?>  Browser.EventTarget
    let svg = 
        D3.Globals.select(node)
            ?attr("width", width + marginLeft + marginRight )
            ?attr("height", height + marginTop + marginBottom )
            ?append("g")
            ?attr("transform", "translate(" + marginLeft.ToString() + "," + marginTop.ToString() + ")" )


    //D3.Globals.Extent doesn't have good method for DateTime, so I used dynamic
    x?domain$( MyD3?extent( model.Data, fun d -> d.Date) ) |> ignore

    //looks like extent is improperly returning a tuple instead of an array so we restructure
    let yMin,yMax = D3.Globals.extent<Datum>( model.Data, System.Func<Datum, float, float>(fun d _ -> d.Close))
    ignore <| y.domain( [|yMin;yMax|] )

    svg?append("g")
        ?attr("class",  "x axis")
        ?attr("transform", "translate(0," + height.ToString() + ")") 
        ?call(xAxis) 
        |> ignore

    svg?append("g")
        ?attr("class", "y axis")
        ?call(yAxis)
        ?append("text")
        ?attr("transform", "rotate(-90)")
        ?attr("y", 6)
        ?attr("dy", ".71em")
        ?style("text-anchor", "end")
        ?text( "Price ($)")
        |> ignore

    svg?append("path")
        ?datum( model.Data )
        ?attr("class", "line")
        ?attr("fill", "none")
        ?attr("stroke", "#000")
        ?attr("d", line)  
        |> ignore

    node?toReact() :?> React.ReactElement<obj>

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
            RT.slider [ Id "range"; SliderProps.Editable true; SliderProps.Min 0.0; SliderProps.Max 1.0; SliderProps.Value 0.0; SliderProps.Step 0.01  ] []
        ]
        R.div [ Style [ GridArea "2 / 3 / 2 / 3"  ] ] [
            R.fn ReactD3 model []
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
