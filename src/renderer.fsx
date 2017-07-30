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
//#r "../node_modules/fable-powerpack/Fable.PowerPack.dll"
#load "../node_modules/fable-import-fetch/Fable.Import.Fetch.fs"
#load "../node_modules/fable-import-fetch/Fable.Helpers.Fetch.fs"
#load "../node_modules/fable-import-react/Fable.Import.React.fs"
#load "../node_modules/fable-import-react/Fable.Helpers.React.fs"
#load "../node_modules/fable-react-toolbox/Fable.Helpers.ReactToolbox.fs"
#load "../node_modules/fable-elmish/elmish.fs"
#load "../node_modules/fable-import-d3/Fable.Import.D3.fs"
#load "graph.fsx"
#load "animation.fsx"
#load "marytts.fsx"
#load "braintrusttasks.fsx"

open System
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.Node
open Fable.Import.Fetch
open Fable.Helpers.Fetch
//we must be at least Fable .7 to use the powerpack!
//open Fable.PowerPack
//open Fable.PowerPack.Fetch
//open Fable.Import.Electron
open Fable.Helpers.ReactToolbox
open Fable.Helpers.React.Props
open Elmish
open Graph
open Animation
open Marytts
open Braintrusttasks
let [<Literal>] ENTER_KEY = 13.

module R = Fable.Helpers.React
module RT = Fable.Helpers.ReactToolbox

type RCom = React.ComponentClass<obj>

type IGinger =
    abstract init: unit -> unit
    abstract doMorph: float -> unit
    abstract selectMorph: string -> unit
let Ginger = importMember<unit->IGinger>("../app/js/ginger.js")
let ginger = Ginger()
//let ReactFauxDOM = importAll<obj> "react-faux-dom/lib/ReactFauxDOM"
//let MyD3 = importAll<obj> "d3"

let Mary : obj = importMember "marytts"
let mary : IMary = createNew Mary ("localhost", 59125) |> unbox
let WebView = importDefault<RCom> "react-electron-webview" 

let braintrustServerURL = "localhost:8080/" 

let inline (!!) x = createObj x
let inline (=>) x y = x ==> y

//Given a uri, retrieve the pagetasks from the server
let GetTaskSet queryUrl =
    let getUrl = braintrustServerURL + "uri?uri=" + queryUrl
    async {
        let! taskSet = fetchAs<TaskSet> (getUrl,[])
        return
            {
                Source = queryUrl
                PageId = -1 //TODO sort out page id; are there implications for this being empty
                Questions = taskSet.questions
                Gist = taskSet.gist
                Prediction = taskSet.prediction
                Triples = taskSet.triples
            }
    } |> Async.RunSynchronously

    //Must be Fable .7 to use the powerpack
    //Fable.PowerPack.Fetch.fetch (getUrl, []) //getUrl []
    // |> Promise.bind( fun res -> res.json())
    // |> Promise.map( fun json ->
    //     //let taskSet = Fable.Import.JS.JSON.parse( json ) |> unbox<TaskSet>
    //     let taskSet = json |> unbox<TaskSet>
    //     {
    //         Source = queryUrl
    //         PageId = -1 //TODO sort out page id; are there implications for this being empty
    //         Questions = taskSet.questions
    //         Gist = taskSet.gist
    //         Prediction = taskSet.prediction
    //         Triples = taskSet.triples
    //     }
    // )
    //()
    

//TODO: share domain model for taskset across client and server
//http://danielbachler.de/2016/12/10/f-sharp-on-the-frontend-and-the-backend.html
let PostTaskSet ( pageTasks : PageTasks ) =
    let postUrl = braintrustServerURL + "uri"
    // let postJson =
    //     !![
    //         "questions" => pageTasks.Questions;
    //         "gist" => pageTasks.Gist;
    //         "prediction" => pageTasks.Prediction;
    //         "triples" => pageTasks.Triples;
    //         "uri" => pageTasks.Source; //TODO check this also has page #
    //     ]
    let formData = Fable.Import.Browser.FormData.Create()
    formData.append("questions", pageTasks.Questions |> Fable.Import.JS.JSON.stringify )
    formData.append("gist", pageTasks.Gist)
    formData.append("prediction", pageTasks.Prediction)
    formData.append("triples", pageTasks.Triples |> Fable.Import.JS.JSON.stringify )
    formData.append("uri", pageTasks.Source )
    //postman tests have been x-www-form-urlencoded
    let props =
        [
            RequestProperties.Method HttpMethod.POST
            RequestProperties.Headers [ContentType "application/x-www-form-urlencoded"]
            RequestProperties.Body (unbox formData )
        ]
    async {
        let! response = fetchAsync(postUrl,props)
        return response
    } |> Async.RunSynchronously
    
    //Must be Fable .7 to use the powerpack  
    // let promise =
    //     //postman tests have been x-www-form-urlencoded
    //     let props =
    //         [
    //             Fetch_types.RequestProperties.Method Fetch_types.HttpMethod.POST
    //             Fetch_types.RequestProperties.Headers [Fetch_types.ContentType "application/x-www-form-urlencoded"]
    //             Fetch_types.RequestProperties.Body (unbox formData )
    //         ]
    //     Fable.PowerPack.Fetch.fetch (postUrl, props) //postUrl props
    //     |> Fable.PowerPack.Promise.bind
    //         (fun response ->
    //             response.json()
    //         )
    //     |> Fable.PowerPack.Promise.catch
    //         (fun err ->
    //           h (ServerResponseError err.Message)
    //         )
    //()


// let fetchEntity url =
//     promise {
//         let! fetched = fetch url []
//         let! response = fetched.text()
//         return response }

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
type TaskState = | Reading | Gist | Prediction | Map | Questions


type Model = {
    count:int
    tabIndex : int
    isChecked : bool
    info : string
    url : string
    MorphValue : float //old way
    MorphName : string //old way
    //idea is to map all visemes into 2 dimensions: jaw height and lip Position
    //probably best way is to use these 2 visemes to create poses for the visemes below and then save out the values
    //http://nir3d.com/handouts/Handouts%203D%20Animation%20II%20Applications%20-%20%28DIG3354C%29/LipSync%20-%20Making%20Characters%20Speak-%20Michael%20B_%20Comet.htm
//    JawHeightMorph : VisemeMorph
//    LipPositionMorph : VisemeMorph
    force: D3.Layout.Force<Link,Node>
    TaskState : TaskState
    PageTasks : PageTasks
    DebugDrawerActive : bool
    TextAnswer : string
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
    | MorphValueChange of float
    | MorphNameChange of string
    | AddNode
    | AddLink
    | Speak of string
    | NextTask
    | Answer of String
    | FinalAnswer 
    | ToggleDebugDrawer

let emptyTask = { Source="error" ; PageId=0; Questions=[||]; Gist="well, I'm not sure"; Prediction="well, I'm not sure"; Triples=[||] }
let emptyModel =  
    {    
        count = 0; 
        tabIndex = 0; 
        isChecked = true; 
        //url = "http://www.google.com"; 
        url = "file:///z/aolney/research_projects/braintrust/materials/NEETS/xhtml/Mod01%20-%20Matter%20Energy%20and%20DC.pdf-extracted/Mod01%20-%20Matter%20Energy%20and%20DC.pdf.xhtml-pretty.html"
        info = ""; 
        MorphValue = 0.1;
        MorphName = "jawrange";
        force  = D3.Layout.Globals.force() :?> D3.Layout.Force<Link,Node> ;
        TaskState = Reading
        PageTasks = emptyTask
        DebugDrawerActive = false
        TextAnswer = ""
    }

//Initialize app and return initial model
let init = function
    | Some savedModel -> savedModel
    | _ -> emptyModel


// UPDATE

///Hacks for demo. We need to know the page number we are on so we can get the knowledge reps for this page
let GetCurrentPage( url : string ) =        
    let bookmarkIndex = url.LastIndexOf("#")
    if bookmarkIndex > 0 then
        let substring = url.Substring(bookmarkIndex + 1, url.Length - bookmarkIndex )
        int substring  //|> unbox<int> 
    else
        -1
let modRegex = new System.Text.RegularExpressions.Regex("Mod\d\d")
let GetMod( modString : string ) = 
    let m = modRegex.Match( modString )
    if m.Success then
        m.Groups.[0].Value
    else
        ""

[<Emit("$0.toString('utf8').replace(/^\uFEFF/, '');")>]
let removeBOM(someUtf:string) : string = failwith "never"
let tasks =
    let json =  Node.fs.readFileSync("app/data/BrainTrustTasks.json", "utf8")
    //because byte order marks are kept! http://stackoverflow.com/questions/24356713/node-js-readfile-error-with-utf8-encoded-file-on-windows
    let pageTasksArr = removeBOM(json) |> JS.JSON.parse |> unbox<PageTasks array>
    pageTasksArr |> Seq.map( fun pt -> (GetMod(pt.Source),pt.PageId), pt) |> Map.ofSeq

let GetPageTasks(url:string) = 
    let m = GetMod(url)
    let p = GetCurrentPage(url)
    match tasks.TryFind( (m,p) ) with
    | Some( task ) -> task
    | None ->     emptyTask

//let t = tasks.[("Mod01",14)]

//END Hacks
let MarySpeak(text:string) = 
    mary.``process``(
        text, 
        createObj[ "base64" ==> true], 
        fun audio -> sound( audio |> unbox<string> )
        //Browser.console.log(audio)  
    )
    mary.durations(
        text, 
        createObj[ "base64" ==> true],
        fun jsDurations  -> 
            let durations = jsDurations |> Array.map( fun d -> { time=unbox<float>(d?time)*1000.0;phoneme=unbox(d?phoneme);number=unbox(d?number) } )
            async{
                //wait a little b/c it takes a sec for audio to render
                do! Async.Sleep( if durations.Length < 10 then 500 else durations.Length * 8 )

                let mutable previousTime = 0.0
                let mutable lastPhoneme = ""
                for d in durations do
                    //need to smooth targets; check out xnagent code for this
                    if Set.contains d.phoneme openPhonemes then
                        ginger.doMorph( 0.3 ) 
                    else 
                        ginger.doMorph( 0.0 )
                    //realised durations is broken; ignore pauses
                    if lastPhoneme <> "_" then 
                        do! Async.Sleep( (d.time - previousTime) |> int )
                    previousTime <- d.time
                    lastPhoneme <- d.phoneme
            } |> Async.StartImmediate
            Browser.console.log(durations)  
    )

let SafePeriod( text : string )=
    if text.EndsWith(".") then text else text + "."
let TriplesToSpeech(triples : Triple array) =
    triples
    |> Seq.map( fun t -> t.start + " " + t.edge + " " + SafePeriod(t.``end``))
    |> Seq.map( fun t -> t.Replace("-LRB-","").Replace("-RRB-","") )
    |> String.concat " "
let GetTaskSpeech(tasks:PageTasks)(state: TaskState)=
    match state with
    | Reading -> [||]
    | Gist  -> [|"Overall I think this is about"; SafePeriod(tasks.Gist) ; "What do you think?" |]
    | Questions when  tasks.Questions.Length > 0 -> [|"I have a question."; tasks.Questions.[0].question |]
    | Map when  tasks.Triples.Length > 0 ->  [|"Alright. So the important things to remember are"; TriplesToSpeech(tasks.Triples); "Does that sound right?"|]
    | Prediction -> [|"OK, the next important thing coming up is probably"; SafePeriod(tasks.Prediction); "Do you agree?" |]
    | _ -> [||]

let CreateNode( label : string ) ( model : Model ) = 
    let x,y = 300.0, 300.0 //totally arbitrary
    let node = {index = None; x = Some(x); y = Some(y); px = None; py = None; ``fixed``=None; weight = None; label=Some(label)}
    model.force.nodes()?push(node) |> ignore
    //return; need reference to create links
    node
let CreateLink( label : string ) ( source : Node ) ( target : Node ) ( model : Model )=
        let link = {source=source;target=target;label=Some(label)}
        model.force.links()?push(link) |> ignore
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
    | MorphValueChange(i) ->
        ginger.doMorph(i)
        {model with MorphValue = i}
    | MorphNameChange(i) ->
        ginger.selectMorph(i)
        {model with MorphName = i}
    | AddNode ->
        //it is important to mutate existing nodes. if we create new ones, e.g. with Array.map, existing links will break
        let x,y = 10.0, 10.0 //totally arbitrary
        let node = {index = None; x = Some(x); y = Some(y); px = None; py = None; ``fixed``=None; weight = None; label=Some("")}
        model.force.nodes()?push(node) |> ignore
        restart( model.force ) |> ignore
        model
    | AddLink ->
        //it is important to mutate existing nodes. if we create new ones, e.g. with Array.map, existing links will break
        let x,y = 10.0, 10.0 //totally arbitrary
        let s = {index = None; x = Some(x); y = Some(y); px = None; py = None; ``fixed``=None; weight = None; label=Some("start")}
        let t = {index = None; x = Some(x); y = Some(y); px = None; py = None; ``fixed``=None; weight = None; label=Some("end")}
        let l = {source=s;target=t;label=Some("edge")}

        model.force.nodes()?push(s) |> ignore
        model.force.nodes()?push(t) |> ignore
        model.force.links()?push(l) |> ignore
        restart( model.force ) |> ignore
        model
    | Speak(text) ->
        MarySpeak(text)
        model
    | NextTask ->
        let tasks = GetTaskSet model.url
        //let tasks = GetPageTasks(model.url)
        let nextTask =
            match model.TaskState with
            | Reading -> Gist
            | Gist when tasks.Questions.Length > 0 -> Questions
            | Questions -> Map
            | Map -> Prediction
            | Prediction -> Reading

            
        //must speak here not in render
        let speech = GetTaskSpeech tasks nextTask |> String.concat " "
        MarySpeak(speech)

        //create map if needed
        let detRegex = System.Text.RegularExpressions.Regex("^(an|the|a) ")
        let NormalizeTriple( text : string)=
            detRegex.Replace( text.Replace("-LRB-","").Replace("-RRB-","").ToLower(),"").Trim()

        if nextTask = Map then
            let cleanTriples = tasks.Triples |> Array.map( fun x -> {start=NormalizeTriple(x.start);edge=x.edge;``end``=NormalizeTriple(x.``end``)} )
            let nodeMap = 
                cleanTriples 
                |> Array.collect( fun x -> [|x.start;x.``end``|])
                |> Array.distinct
                |> Array.map( fun x -> x, CreateNode x model )
                |> Map.ofArray
            for triple in cleanTriples do
                CreateLink triple.edge nodeMap.[triple.start] nodeMap.[triple.``end``] model
            
            (*for triple in tasks.Triples do
                let source = CreateNode triple.Start model
                let target = CreateNode triple.End model
                CreateLink triple.Edge source target model*)
            restart( model.force ) |> ignore

        //update the server; not sure this is the best place, see "FinalAnswer" below
        if nextTask = Reading && model.PageTasks <> emptyTask then
             PostTaskSet model.PageTasks |> ignore

        {model with TaskState=nextTask; PageTasks=tasks}
    | FinalAnswer ->
        //TODO record their answer, kick off next task?
        model
    | Answer(i) ->
        let tasks = 
            match model.TaskState with
            | Gist -> {model.PageTasks with Gist=i}
            | Prediction -> {model.PageTasks with Prediction=i}
            | Questions -> 
               let qaPairArr = [|{model.PageTasks.Questions.[0] with answer=i}|]
               {model.PageTasks with Questions=qaPairArr}
        {model with PageTasks=tasks; TextAnswer=i}
    | ToggleDebugDrawer ->
        match model.DebugDrawerActive with
        | true -> {model with DebugDrawerActive=false}
        | false -> {model with DebugDrawerActive=true}
    | _ -> model
// VIEW
let internal onEnter msg dispatch =
    function 
    | (ev:React.KeyboardEvent) when ev.keyCode = ENTER_KEY ->
        ev.preventDefault() 
        dispatch msg
    | _ -> ()
    |> OnKeyDown

let internal onMouseClick msg dispatch =
    function 
    | (ev:React.MouseEvent) ->
        ev.preventDefault() 
        dispatch msg
    | _ -> ()

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

/// The left pane is always the browser, which is the reading area
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
/// The agent view must always be mounted, so when not in use it should be hidden
let viewAgent model dispatch =
    R.div [ Hidden (model.TaskState = Reading) ] [
    //R.div [ ] [
        R.div [ Id "renderer"] []
        //R.com<GingerAgent,_,_> [][] //we create a component so we can control should update
        //R.p [] [ unbox "Alright. So the important thing to remember is that ..."]
        //fun i -> MorphNameChange(unbox<string> i) |> dispatch );
        RT.iconMenu  [ IconMenuProps.OnSelect(  unbox >> MorphNameChange >> dispatch );  Id "morph" ; IconMenuProps.Icon (U2.Case2 "more_vert"); IconMenuProps.Position "topLeft" ] [
            RT.menuItem [ MenuItemProps.Value "eyes"; MenuItemProps.Caption "Eyes"  ] [  ]
            RT.menuItem [ MenuItemProps.Value "expression"; MenuItemProps.Caption "Expression"  ] [  ]
            RT.menuItem [ MenuItemProps.Value "jawrange"; MenuItemProps.Caption "Jaw Height"  ] [  ]
            RT.menuItem [ MenuItemProps.Value "jawtwist"; MenuItemProps.Caption "Jaw Twist"  ] [  ]
            RT.menuItem [ MenuItemProps.Value "symmetry"; MenuItemProps.Caption "Symmetry"  ] [  ]
            RT.menuItem [ MenuItemProps.Value "lipcurl"; MenuItemProps.Caption "Lip Curl"  ] [  ]
            RT.menuItem [ MenuItemProps.Value "lipsync"; MenuItemProps.Caption "Lip Sync"  ] [  ]
            RT.menuItem [ MenuItemProps.Value "sex"; MenuItemProps.Caption "Face Structure"  ] [  ]
            RT.menuItem [ MenuItemProps.Value "width"; MenuItemProps.Caption "Jaw Width"  ] [  ]
            RT.menuItem [ MenuItemProps.Value "tongue"; MenuItemProps.Caption "Tongue"  ] [  ]
            //RT.menuItem [ MenuItemProps.Value "teethopentop"; MenuItemProps.Caption "Teeth Open Top"  ] [  ]
            //RT.menuItem [ MenuItemProps.Value "teethopenbot"; MenuItemProps.Caption "Teeth Open Bot"  ] [  ]
            //RT.menuItem [ MenuItemProps.Value "teethsidebot"; MenuItemProps.Caption "Teeth Side Bot"  ] [  ]
            //RT.menuItem [ MenuItemProps.Value "teethsidetop"; MenuItemProps.Caption "Teeth Side Top"  ] [  ]
        ]
        //SliderProps.OnChange ( MorphValueChange >> dispatch ); //needs JS.Function?
        //SliderProps.Value model.MorphValue; //need to be able to change model
        //RT.slider [ SliderProps.Value model.MorphValue; SliderProps.OnChange ( JS.Function.Create("i","function ($var11) { return dispatch(function (arg0) {return new Msg('MorphValueChange', [arg0]);}($var11));}") ); Id "range"; SliderProps.Editable true; SliderProps.Min 0.0; SliderProps.Max 1.0;   SliderProps.Step 0.01  ] []
        RT.slider [ SliderProps.Value model.MorphValue; SliderProps.OnChange ( MorphValueChange >> dispatch ); Id "range"; SliderProps.Editable true; SliderProps.Min 0.0; SliderProps.Max 1.0;   SliderProps.Step 0.01  ] []
        //RT.button [ Label "Speak"; Raised true; onClick Speak dispatch ] []
        //RT.button [ Label "Add Node"; Raised true; onClick AddNode dispatch ] []
    ]
let viewMapTask model dispatch =
    let speech = GetTaskSpeech model.PageTasks model.TaskState |> String.concat " "

    R.div [ Style [ GridArea "2 / 3 / 2 / 3"  ] ] [
        R.p [
            //TODO: move font styles to css CSSProp.MarginTop (U2.Case2 "1cm"); 
            Style [ CSSProp.FontFamily (U2.Case1 "'Roboto', sans-serif"); CSSProp.FontSize( U2.Case2 "1em"); ]
        ] [ unbox speech]
        R.div [ ]  [
            RT.button [ Label "Add Node"; Raised true; onClick AddNode dispatch ] []
            RT.button [ Label "Add Link"; Raised true; onClick AddLink dispatch ] []
            RT.button [ Label "Done"; Raised true; onClick NextTask dispatch ] []
        ]
        R.com<ForceDirectedGraph,_,_> 
            { new ForceDirectedGraphProps with
                member __.force = model.force
            } []
    ]
    
let viewReadTask model dispatch = 
    //R.div [ Style [  CSSProp.Display (U2.Case1 "table"); CSSProp.Width (U2.Case2 "100%"); CSSProp.Height (U2.Case2 "100%");] ] [
     //R.div [ Style [  CSSProp.TextAlign (U2.Case1 "center"); CSSProp.Width (U2.Case2 "500px"); ] ] [
         //went absolute with the button; nothing else seemed to work
            RT.button [ Label "Teach"; Raised true; onClick NextTask dispatch;  
                Style [CSSProp.Margin (U2.Case1 "0 auto");   CSSProp.Display (U2.Case1 "block"); CSSProp.Position (U2.Case1 "absolute"); CSSProp.Right (U2.Case2 "25%"); CSSProp.Top (U2.Case2 "50%"); CSSProp.Transform (U2.Case1 "translateY(-50%)");] ] []
    //]
    //]

let viewGistTask model dispatch =
    let speech = GetTaskSpeech model.PageTasks model.TaskState 
    let prefix = speech.[0]
    let suffix = speech.[2]
    (*
    let pageTasks = GetPageTasks(model.url) 
    let prefix = "Overall I think this is about"
    let suffix = "What do you think?"
    MarySpeak( prefix + " " + model.PageTasks.Gist + " " + suffix)
    *)
    //Info(pageTasks.Gist) |> dispatch //VERY BAD
    R.div [ Style [  CSSProp.MarginTop (U2.Case2 "2cm"); ]]  [
        R.p [
            //TODO: move font styles to css
            Style [  CSSProp.FontFamily (U2.Case1 "'Roboto', sans-serif"); CSSProp.FontSize( U2.Case2 "1em"); ]
        ] [ unbox prefix]
        RT.input [ Type "text"; Label "answer here"; InputProps.Value model.PageTasks.Gist; InputProps.OnChange ( Answer >> dispatch ); onEnter NextTask dispatch ] []
        R.p [
            Style [  CSSProp.FontFamily (U2.Case1 "'Roboto', sans-serif"); CSSProp.FontSize( U2.Case2 "1em"); ]
        ] [ unbox suffix]
    ]

let viewPredictionTask model dispatch =
    let speech = GetTaskSpeech model.PageTasks model.TaskState 
    let prefix = speech.[0]
    let suffix = speech.[2]
(*
    let pageTasks = GetPageTasks(model.url) 
    let prefix = "OK, the next important thing coming up is probably"
    let suffix = "Do you agree?"
    MarySpeak( prefix + " " + model.PageTasks.Prediction + " " + suffix)
    *)
    //Info(pageTasks.Gist) |> dispatch //VERY BAD
    R.div [ Style [  CSSProp.MarginTop (U2.Case2 "2cm"); ]]  [
        R.p [
            //TODO: move font styles to css
            Style [  CSSProp.FontFamily (U2.Case1 "'Roboto', sans-serif"); CSSProp.FontSize( U2.Case2 "1em"); ]
        ] [ unbox prefix]
        RT.input [ Type "text"; Label "answer here"; InputProps.Value model.PageTasks.Prediction; InputProps.OnChange ( Answer >> dispatch ); onEnter NextTask dispatch ] []
        R.p [
            Style [  CSSProp.FontFamily (U2.Case1 "'Roboto', sans-serif"); CSSProp.FontSize( U2.Case2 "1em"); ]
        ] [ unbox suffix]
    ]

let viewQuestionTask model dispatch =
    let speech = GetTaskSpeech model.PageTasks model.TaskState 
    let prefix = speech.[0]
    let question = speech.[1]
(*
    let pageTasks = GetPageTasks(model.url) 
    let prefix = "I have a question."
    MarySpeak( prefix + " " + model.PageTasks.Questions.[0].Question )
    let question = 
        if model.PageTasks.Questions.Length > 0 then
            model.PageTasks.Questions.[0].Question
        else
            "No, nevermind."
            *)
    //Info(pageTasks.Gist) |> dispatch //VERY BAD
    R.div [ Style [  CSSProp.MarginTop (U2.Case2 "2cm"); ]]  [
        R.p [
            //TODO: move font styles to css
            Style [  CSSProp.FontFamily (U2.Case1 "'Roboto', sans-serif"); CSSProp.FontSize( U2.Case2 "1em"); ]
        ] [ unbox ( prefix + " " + question) ]
        //InputProps.Value question;
        RT.input [ Type "text"; Label "answer here";  InputProps.Value model.TextAnswer ; InputProps.OnChange ( Answer >> dispatch ); onEnter NextTask dispatch ] []
    ]

[<Emit("JSON.stringify($0, undefined, 2)")>]
let prettyStringify (json): string = jsNative

/// The right pane changes depending on the task; we could decompose it into two panes
let viewRightPane model dispatch = 
    R.div [] [
        R.div [ Style [ GridArea "1 / 2 / 1 / 2"  ] ] [
            R.div [ Hidden ( not <| model.DebugDrawerActive) ] [
                  R.pre[ Style[CSSProp.Color (U2.Case2 "red")] ] [
                       (prettyStringify model) |> unbox
                    ]
             ]

            //debug drawer doesn't work
            (*
            RT.navDrawer [ NavDrawerProps.Width "wide"; NavDrawerProps.Active model.DebugDrawerActive; 
                NavDrawerProps.OnOverlayClick(onMouseClick ToggleDebugDrawer  dispatch) ] [ 
                    R.pre[ Style[CSSProp.Color (U2.Case2 "red")] ] [
                       (prettyStringify model) |> unbox
                    ]
            ]*)

            //agent
            (viewAgent model dispatch)

            //state dependent task
            (match model.TaskState with
            | Reading ->  viewReadTask model dispatch 
            | Gist -> viewGistTask model dispatch
            | Questions -> viewQuestionTask model dispatch
            | Map -> viewMapTask model dispatch
            | Prediction -> viewPredictionTask model dispatch
            //| _ -> viewMapTask model dispatch
            )

            //trigger debug window
            RT.button [ Label "Debug"; Raised true; onClick ToggleDebugDrawer dispatch;
            Style [ CSSProp.Position (U2.Case1 "absolute"); CSSProp.Right (U2.Case2 "1%"); CSSProp.Bottom (U2.Case2 "1%"); ] ] []
            
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
        ginger.selectMorph("jawrange")

        this.props <- true

    member this.render() =
        viewMain this.state dispatch

ReactDom.render(
        R.com<App,_,_> () [],
        Browser.document.getElementById("app")
    ) |> ignore

//webpack issues
// need     "ajv":"^5.2.2" to handle "Module parse failed"
// also update this in package.json of node_modules/har_validator and run npm install from there