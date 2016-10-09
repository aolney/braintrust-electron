# BrainTrust
BrainTrust is a university research project that attempts to solve the knowledge engineering bottleneck for intelligent tutoring systems.

When a human student navigates to an online textbook using BrainTrust, the application generates a virtual student.

As the human student teaches the virtual student, they engage in reading comprehension activities that

- Help the human student better understand the text
- Create knowledge representations for a future intelligent tutoring system for that text

This BrainTrust client uses a variety of web-technologies in order to be cross platform:

- [Electron](https://github.com/electron/electron-quick-start)
- [Fable](http://fable.io)
- [Elm](http://elm-lang.org/)
- [React](https://facebook.github.io/react/)
- [React Toolbox](http://react-toolbox.com/)

Specificaly, BrainTrust is an electron app using fable to transpile js and vscode to debug the electron process using source maps, together with elmish UI using react and react-toolbox for rendering.

Small, minimal functional examples of how these technologies can be combined along with reference tutorials, are:

- [Fable Electron Hello World](https://github.com/aolney/fable-electron-vscode-debug-helloworld)
- [Fable Electron Elmish React/Toolbox](https://github.com/aolney/fable-electron-elmish-react-reacttoolbox)

## To Use

To clone and run this repository you'll need [Git](https://git-scm.com) and [Node.js](https://nodejs.org/en/download/) (which comes with [npm](http://npmjs.com)) installed on your computer. Some linux distributions call `node` something else, e.g. `nodejs`, which may cause you problems.

To use VSCode you will need to [install it](https://code.visualstudio.com/download).

I've also installed these extensions:

- [Debugger for Chrome](https://marketplace.visualstudio.com/items/msjsdiag.debugger-for-chrome). It must be version 1.1.0 or greater to allow breakpoints to be set in F#, see [here](https://github.com/octref/vscode-electron-debug/issues/2#issuecomment-251800254)
- [Ionide-fsharp](https://marketplace.visualstudio.com/items/Ionide.Ionide-fsharp). Not strictly necessary but recommended for intellisense.

From your command line:

1. Clone this repository

   `git clone https://github.com/aolney/braintrust-electron.git`

2. Build and Run

    * From command line
      1. Go into the repository

         `cd braintrust-electron`

      2. Run fable (fableconfig.json contains the compiler options)

         `./node_modules/.bin/fable`

      3. Run electron

         `npm start`

    * From VSCode
      1. Start VSCode

         `code braintrust-electron`
      2. Run the build task

         `View -> Command Palette ; type run build task`
      3. Put a breakpoint in main.js or renderer.js, e.g. inside `createMainWindow`
      4. Enter debug mode, select main or render debug dropdown depending on breakpoint location
      5. Run the debugger. Debugger will break at F# line corresponding to the location of your js breakpoint. You may need to `ctrl-r` in Electron to refresh and hit breakpoints in render process, see [here](http://code.matsu.io/1)
      6. Once the debugger has hit the breakpoint you can set other breakpoints in F# that will function. These breakpoints appear to be in the sourcemap. If you set breakpoints in the F# source proper they will not function as expected and show up as greyed out breakpoints in the debug view while the debug session is running.

      **You can also run the `watch` task so that files are transpiled automatically on save. Enable by View -> Command Palette -> Type `tasks: run task` -> Select `watch`. Or ctrl-p and type `task watch`**

#### License [Apache-2.0](LICENSE.md)
