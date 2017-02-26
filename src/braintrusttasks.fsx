
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

type QAPair =
    {
        Question : string;
        Answer : string;
    }

type Triple =
    {
        Start : string
        Edge : string
        End : string
    }

type PageTasks =
    {
        Source : string
        PageId : int
        Questions : QAPair array
        Gist : string
        Prediction : string
        Triples : Triple array
    }