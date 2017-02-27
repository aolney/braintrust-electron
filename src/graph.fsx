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

//NOTE: named ddd to avoid namespace collision with D3

#r "../node_modules/fable-core/Fable.Core.dll"
#load "../node_modules/fable-import-d3/Fable.Import.D3.fs"
#load "../node_modules/fable-import-react/Fable.Import.React.fs"
#load "../node_modules/fable-import-react/Fable.Helpers.React.fs"

open System
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import

module R = Fable.Helpers.React

type RCom = React.ComponentClass<obj>
let Component = importMember<RCom> "react-d3-library"


//note Node/Link conform to interface definitions in D3.Layout.Force
//although f# does not have implicit interface implementations, the conversion to javascript seems to de facto do this
type Node =
    {
        index : float option
        x : float option
        y : float option
        px : float option
        py : float option
        ``fixed``: bool option
        weight: float option
        label : string option
    }

//for directionality we replace link with new link where source and target are reversed
type Link =
    {
        source : Node
        target : Node
        label : string option
    }


//UI handlers
type UIState =
    {
        selectedNode : Node option
        mousedownNode : Node option
        mouseupNode : Node option
        selectedLink : Link option
        mousedownLink : Link option
        keyCode : int option
    }
let initialState = {keyCode=None;selectedNode=None;mousedownNode=None;mouseupNode=None;selectedLink=None;mousedownLink=None}
let mutable uiState = initialState
let resetMouseState() = 
    uiState <- {uiState with mousedownLink=None;mousedownNode=None;mouseupNode=None}
let rec restart(force : D3.Layout.Force<Link,Node>) =
    let svg = D3.Globals.select("svg")

    let links = svg?selectAll(".link")?data(force.links())

    links
        ?enter()
        ?insert("svg:line")
        ?attr("class", "link")
        ?attr("marker-end",  "url(#end-arrow)" )
        ?on("mousedown", fun d ->
            uiState <- {uiState with mousedownLink=Some(d);selectedLink=Some(d);selectedNode=None} 
            restart( force )
        )
        |> ignore

    //clear any links we deleted from layout
    links?exit()?remove() |> ignore

    //highlight selected link
    links
        ?classed("link_selected", fun d -> 
            match uiState with
            | {selectedLink=Some(link)} when d = link -> true
            | _ -> false
        )
        |> ignore

    let nodes = svg?selectAll(".node")?data(force.nodes())

    nodes
        ?enter()
        ?insert("svg:circle")
        ?attr("class", "node")
        ?attr("r", 5)
        (*removed for demo
        ?on("mousedown", fun d ->
            uiState <- {uiState with mousedownNode=Some(d);selectedNode=Some(d);selectedLink=None;} 
            //restart( force )
            )
        ?on("mouseup", fun d ->
            uiState <- {uiState with mouseupNode=Some(d);selectedNode=Some(d);} 
            //restart( force )
            )
            *)
        //uncommented for demo
        ?call(force.drag()) //this will make a node draggable
        |> ignore
    
    //do labels
    let nodeLabels = svg?selectAll(".node_label")?data(force.nodes())

    nodeLabels
        ?enter()
        ?append("text")
        ?attr("class", "node_label")
        ?attr("x", 10)
        ?attr("y", ".35em")
        ?text( fun d ->  d.label )
        ?style("stroke", "#FF0000")
        |> ignore

    let linkLabels = svg?selectAll(".link_label")?data(force.links())

    linkLabels
        ?enter()
        ?append("text")
        ?attr("class", "link_label")
        ?attr("x", 10)
        ?attr("y", ".35em")
        ?text( fun d ->  d.label )
        ?style("stroke", "#FF0000")
        |> ignore

    //clear any nodes we deleted from layout
    nodes?exit()?remove() |> ignore
 
    //highlight selected nodes
    nodes
        ?classed("node_selected", fun d -> 
            match uiState with
            | {selectedNode=Some(node)} when d = node -> true
            | _ -> false
        )
        |> ignore

    force.start();

let mousemove( force : D3.Layout.Force<Link,Node> ) =
    //current mouse coordinates
    let mouseX,mouseY =  D3.Globals.mouse(Browser.``event``.currentTarget)

    //move circle cursor (eventually delete this)
    D3.Globals.select(".cursor")
        ?attr("transform", "translate(" + unbox<string>(mouseX) + "," + unbox<string>(mouseY) + ")")
        |> ignore

    match uiState with
    //node; continue to drag line
    | {mousedownNode=Some(n)} ->
        let x,y =
            match n.x,n.y with
            | Some(x),Some(y) -> x,y
            | _ -> 0.0,0.0
        D3.Globals.select(".drag_line")
            ?attr("d", "M" + unbox<string>(x) + "," + unbox<string>(y) + "L" + unbox<string>(mouseX) + "," + unbox<string>(mouseY))
            |> ignore

        //restart( force ) |> ignore
    //all other cases do nothing
    | _ -> ()


let mousedown(force : D3.Layout.Force<Link,Node> ) =
    match uiState with
    //empty space md; deselect everything -- should we reset here?
    | {mousedownNode=None; mousedownLink=None} ->
        uiState <- {uiState with selectedNode=None; selectedLink=None }
    //node; change appearance and prepare to drag line
    | {mousedownNode=Some(node)} ->
        uiState <- {uiState with selectedNode=Some(node); selectedLink=None }

        //drag line
        let x,y =
            match node.x,node.y with
            | Some(x),Some(y) -> x,y
            | _ -> 0.0,0.0

        D3.Globals.select(".drag_line_hidden")
            // ?style("marker-end", "url(#end-arrow)")
            // ?classed("hidden", false)
            ?attr("class","drag_line")
            ?attr("d", "M" + unbox<string>(x) + "," + unbox<string>(y) + "L" + unbox<string>(x) + "," + unbox<string>(y))
            |> ignore
    //link
    | {mousedownLink=Some(link)} ->
        uiState <- {uiState with selectedNode=None; selectedLink=Some(link) }
    //all other cases do nothing
    | _ -> ()


let mouseup(force : D3.Layout.Force<Link,Node> ) =
    //hide drag line
    D3.Globals.select(".drag_line")
            ?attr("class","drag_line_hidden")
            |> ignore

    match uiState with
    //down/up on same node; assume aborted action
    | {mousedownNode=Some(mD); mouseupNode=Some(mU)} when mD=mU ->
        resetMouseState()
    //up on another node; make link
    | {mousedownNode=Some(mD); mouseupNode=Some(mU)} ->
        let link = { source=mD; target=mU; label=None }
        force.links()?push(link) |> ignore
        uiState <- {uiState with selectedLink=Some(link); selectedNode=None}
    //up in empty space; make node
    | {mouseupNode=None; mousedownLink=None} ->
        let x,y =  D3.Globals.mouse(Browser.``event``.currentTarget)
        let node = {index = None; x = Some(x); y = Some(y); px = None; py = None; ``fixed``=None; weight = None; label=None}
        force.nodes()?push(node) |> ignore
        uiState <- {uiState with selectedNode=Some(node); selectedLink=None}
    //all other cases do nothing
    | _ -> ()

    //clear mouse state
    resetMouseState()

    //generally safe to assume we should restart force layout
    restart( force )

let spliceLinksForNode links node =
        let toSplice = links?filter(fun (l : Link)->
                (l.source = node || l.target = node);
        )
        toSplice?map(fun l ->
            links?splice(links?indexOf(l), 1);
        )
        |> ignore

let keydown(e: Browser.KeyboardEvent, force : D3.Layout.Force<Link,Node> ) =

    let keyCode = int e.keyCode
    uiState <- {uiState with keyCode=Some(keyCode)}
       
    let nodes = force.nodes()
    let links = force.links()

    match uiState with
    //selected node with delete key
    | {selectedNode=Some(node); keyCode=Some(keyCode)} when keyCode = 46 ->
        nodes?splice( nodes?indexOf( node ), 1 ) |> ignore
        spliceLinksForNode links node
        uiState <- {uiState with keyCode=None; selectedNode=None}
        restart( force ) |> ignore
    //selected link with delete key
    | {selectedLink=Some(link); keyCode=Some(keyCode)} when keyCode = 46 ->
        links?splice( links?indexOf( link ), 1 ) |> ignore
        uiState <- {uiState with keyCode=None; selectedLink=None}
        restart( force ) |> ignore
    //all other cases do nothing
    | _ -> ()

    //required to return object?
    null

//------------------------------------------------------------------
//THIS IS NOW JUNK
(*
let click(force : D3.Layout.Force<Link,Node> ) =
    let x,y =  D3.Globals.mouse(Browser.``event``.currentTarget)

    let node = {index = None; x = Some(x); y = Some(y); px = None; py = None; ``fixed``=None; weight = None}
    let nodes = force.nodes()
    let links = force.links() 
    nodes?push(node) |> ignore

    // add links to any nearby nodes; exclude self
    nodes?forEach( fun target -> 
        let x = 
            match target.x, node.x with
            | Some(t), Some(n) -> t-n
            | _,_ -> 0.0

        let y = 
            match target.y, node.y with 
            | Some( t), Some(n) -> t-n
            | _,_ -> 0.0

        if x <> 0.0 && y <> 0.0 && Math.Sqrt(x * x + y * y) < 30.0 then
            links?push({source= node; target= target}) |> ignore
    ) |> ignore
    
    restart( force )
    *)

//this is called exactly once to initialize the d3 graph for react
let createForceGraph( force : D3.Layout.Force<Link,Node> ) =
    let graph = Browser.document.createElement_div()

    let width = 700
    let height = 700

    //configure the force. Note the empty model force is not configured and has goofy defaults
    //force?charge(-500)?linkDistance(150)?size([|width; height|]) |> ignore
    force?charge(-500)?linkDistance(150)?gravity(0.05)?size([|width; height|]) |> ignore


    let svg = 
        D3.Globals.select(graph)
            ?append("svg:svg")
                ?attr("width", width )
                ?attr("height", height )
                //special handler; part of react-d3-library pattern
            ?on("mount",fun () ->

        //react-d3-library requires that we reselect
        let svg = D3.Globals.select("svg")

        //link with arrow
        svg
            ?append("defs")
            ?append("marker")
                ?attr("id", "end-arrow")
                ?attr("viewBox", "0 -5 10 10")
                ?attr("refX", 13)
                ?attr("markerWidth", 3)
                ?attr("markerHeight", 3)
                ?attr("orient", "auto")
            ?append("path")
                ?attr("d", "M0,-5L10,0L0,5")
                |> ignore

        //drag line; initially hidden; we carefully alternate between hidden and not
        svg
            ?append("svg:path")
                ?attr("class", "drag_line_hidden")
                ?attr("d", "M0,0L0,0")
                |>ignore

        //circle cursor
        svg
            ?append("svg:circle")
            ?attr("r", 30)
            ?attr("transform", "translate(-100,-100)")
            ?attr("class", "cursor")
            |> ignore
        
        force.on("tick", fun _ ->
            svg
                ?selectAll(".link")
                ?attr("x1", fun (d : Link) -> d.source.x )
                ?attr("y1", fun d ->  d.source.y)
                ?attr("x2", fun d ->  d.target.x)
                ?attr("y2", fun d ->  d.target.y)
                |> ignore

            svg
                ?selectAll(".node")
                ?attr("cx", fun d ->  d.x)
                ?attr("cy", fun d ->  d.y)
                |> ignore

            //labels
            svg
                ?selectAll(".node_label") 
                //?selectAll("text") 
                ?attr("x", fun d ->  d.x)
                ?attr("y", fun d ->  d.y)
                |> ignore

            
            svg
                ?selectAll(".link_label") 
                ?attr("x", fun d ->  
                    match d.source.x, d.target.x with
                    | Some(x1),Some(x2) -> (x1 + x2) / 2.0
                    | _,_ -> 0.0 )
                ?attr("y", fun d -> 
                    match d.source.y, d.target.y with
                    | Some(x1),Some(x2) -> (x1 + x2) / 2.0
                    | _,_ -> 0.0 )
                |> ignore

        ) |> ignore


        //svg level event handlers
        svg
            ?on("mousemove", fun _ -> mousemove( force ) ) //we need force in the closure
            (*removed for demo
            ?on("mousedown", fun _ -> mousedown( force ) ) //we need force in the closure
            ?on("mouseup", fun _ -> mouseup( force )) //we need force in the closure
            *)
            //?on("keydown", fun _ -> keydown( force )) //we need force in the closure
            //?on("click", fun _ -> click( force )) //we need force in the closure
            |> ignore
        
        Browser.window.addEventListener_keydown(fun e -> keydown(e, force))
    )
    //react-d3-library pattern has use return the dom element
    graph
    

//Because we want to manipulate force programatically, it is global state we pass in as props
type ForceDirectedGraphProps =
    abstract force : D3.Layout.Force<Link,Node>

type ForceDirectedGraphState = 
    {
        //state we truly don't touch - d3 dom is isolated
        d3 : Browser.HTMLDivElement
        //we want the d3 dom to render only once on mount
        mounted : bool
        //on mount, we supply force to d3 through state
        force : D3.Layout.Force<Link,Node>
    }

type ForceDirectedGraph(props, ctx) as this =
    inherit React.Component<ForceDirectedGraphProps, ForceDirectedGraphState>(props, ctx)
    do this.state <- { d3 = null; mounted = false; force = props.force}

    //the react-d3-library pattern calls for d3 to be initialized when component mounts
    member this.componentDidMount() =
        this.setState {this.state with mounted=true; d3 = createForceGraph(this.state.force)  }
        
    //re-rendering will destroy any mutation we've done in d3 since mounting
    member this.shouldComponentUpdate () = not <| this.state.mounted

    member this.render() =
        R.div [] 
            [
                //the react-d3-library pattern calls for d3 element to be passed in as prop called data
                React.createElement( Component, createObj[ "data" ==> this.state.d3 ], [||] ) 
            ]