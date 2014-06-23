open System
open System.Collections.Generic;

type ReleaseGroup =  {
    ID : string;
    Title : string;
    Type : string;
    SecondaryType : string;
}

type Artist = {
    ID : string;
    Name : string;
}

type Recording = {
    ID : string;
    Title : string;
    Artist : List<Artist>;
    ReleaseGroups : List<ReleaseGroup>;
}

let main = 
    Console.WriteLine("Hello world!")