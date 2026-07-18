"use client";

import { useState, Dispatch, SetStateAction } from "react";
import { TerminalEntry, TerminalResponse } from "@/lib/TerminalResponse";


type TerminalScreenProps = {
    path: string;
    setPath: Dispatch<SetStateAction<string>>;

    setMode: Dispatch<SetStateAction<"shell" | "editor">>;

    setEditor: Dispatch<
        SetStateAction<{
            file: string;
            language: string;
            content: string;
        }>
    >;
};


export default function TerminalScreen({
    path,
    setPath,
    setMode,
    setEditor
}: TerminalScreenProps) {

    const [command, setCommand] = useState("");

    const [history, setHistory] = useState<
        TerminalEntry[]
    >([]);


    async function execute() {

        if(command.trim() === "")
            return;


        console.log("Executing:", command);


        const response = await fetch(
            "http://localhost:5245/api/terminal",
            {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    command
                })
            }
        );


        const data: TerminalResponse =
            await response.json();


        console.log(
            JSON.stringify(data, null, 2)
        );


        if(data.mode === "editor")
        {
            setEditor({
                file: data.file ?? "",
                language: data.language ?? "plaintext",
                content: data.content ?? ""
            });

            setMode("editor");
        }
        else
        {
            // C# controls the history now
            setHistory(data.history ?? []);
        }


        if(data.currentPath)
        {
            setPath(data.currentPath);
        }


        setCommand("");
    }


    return (
        <div className="
            bg-black
            h-screen
            w-full
            p-5
            text-white
            font-mono
        ">


            <div className="mb-2">

                {history.map((entry, index) => (

                    <div
                        key={index}
                        className="mb-2"
                    >

                        <p>
                            {entry.path}

                            <span className="text-amber-400">
                                {" $ "}
                            </span>

                            {entry.command}
                        </p>


                        <pre className="
                            whitespace-pre-wrap
                        ">
                            {entry.output}
                        </pre>


                    </div>

                ))}

            </div>


            <div>

                {path}

                <span className="text-amber-400">
                    {" $ "}
                </span>


                <input
                    className="
                        bg-transparent
                        outline-none
                        ml-2
                        w-5/6
                    "

                    autoFocus

                    value={command}

                    onChange={(e)=>
                        setCommand(e.target.value)
                    }


                    onKeyDown={(e)=>
                    {
                        if(e.key === "Enter")
                        {
                            execute();
                        }
                    }}
                />

            </div>


        </div>
    );
}