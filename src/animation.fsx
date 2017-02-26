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

///Viseme morph state
type VisemeMorph =
 {
     Duration : double;
     TimeElapsed : double;
     Tween : double;
     Model : string;
 }

///Given a viseme morph as state and elapsed time, returns an updated morph
let UpdateVisemeMorph (morph : VisemeMorph) (elapsed : double) =
    let timeElapsed = morph.TimeElapsed + elapsed
    let tween =
        //are we still increasing
        if timeElapsed < morph.Duration then
            //linearly increase approaching 1.0
            timeElapsed / morph.Duration
        else 
            0.0
    {morph with TimeElapsed=timeElapsed; Tween=tween}

///Expression morph state
type ExpressionMorph =
 {
     Duration : double;
     Tween : double;
     Model : string;
     RiseTime : double;
     SustainTime : double;
     DecayTime : double;
     TimeElapsed : double;
 }

///Given an expression morph as state and elapsed time, returns an updated morph
let UpdateExpressionMorph (morph : ExpressionMorph) (elapsed : double) =
    let timeElapsed = morph.TimeElapsed + elapsed
    let tween =
        //is the morph done
        if timeElapsed < morph.Duration then
            //are we still rising in intensity
            if timeElapsed < morph.RiseTime then
                timeElapsed / morph.Duration
            //are we sustaining
            else if timeElapsed < morph.SustainTime then
                morph.Tween
            //are we decaying in intensity
            else if timeElapsed < morph.DecayTime then
                (morph.Duration - timeElapsed) / morph.DecayTime
            else
                0.0 //should never happen but keeps the compiler happy
        else 
            0.0
    {morph with TimeElapsed=timeElapsed; Tween=tween}

(*
#meshName	sapiVisemeId
AA	2
EH	4,11,12
ER	5,13
F	18
IH	1
IY	6
K	20
L	14
M	21
OW	8,9
S	15
SH	16
T	19
TH	17
UW	3,10
W	7
#-----------------------------
#collapsed mapping
#a h ate
#e i eat
#l
#r
#o u two
#p b m
#g k d n t y
#f v w
#h j s z
#sh ch
#th
#silence
#----------------------------
#fuller mapping
#ax ah ae -- pat/abs/hut/cut/say
#aa -- odd/bob
#ao -- ought
#ey eh uh ae -- egg/ate/but/hood
#er -- hurt
#y iy ih ix -- yes/yield/see/beat/it
#w uw u -- win/we/two/boot/you
#ow -- oh/oat
#aw -- how/cow
#oy -- boy
#ay -- I/hide/bite
#h -- house/he
#r -- rise/her/read
#l -- let/lee
#s z ts -- small zoo tsuzuki
#sh ch jh zh -- shall/childe/gee/she
#th dh -- think/thee/theta/then
#f v -- flowers/very
#d t dx n -- dig/team/butter/name
#k g ng -- call/give/sing
#p b m -- pen/boy/main
*)