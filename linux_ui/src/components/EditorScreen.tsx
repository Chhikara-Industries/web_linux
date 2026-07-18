"use client";

import Editor, { OnMount } from "@monaco-editor/react";
import * as monaco from "monaco-editor";
import { useState } from "react";

type EditorScreenProps = {
    language: string;
    file: string;
    content: string;

    onExit: () => void;
};

export default function EditorScreen({
    language,
    file,
    content,
    onExit,
}: EditorScreenProps) {
    
    const [code, setCode] = useState(content);

    const handleEditorDidMount: OnMount = (editor) => {
        console.log("Monaco Mounted!")
        // Ctrl + Q -> Exit editor
        editor.addCommand(
            monaco.KeyMod.CtrlCmd | monaco.KeyCode.KeyQ,
            () => {
                console.log("Ctrl + Q pressed!")
                onExit();
            }
        );

        // Ctrl + S -> Save (placeholder)
        editor.addCommand(
            monaco.KeyMod.CtrlCmd | monaco.KeyCode.KeyS,
            () => {
                console.log("Ctrl + S pressed!")
                console.log("Saving...");
                // TODO: Send file to C# backend
            }
        );
    };

    return (
        <div className="w-full h-screen flex flex-col bg-black">

            <div className="px-3 py-2 bg-zinc-900 text-white border-b border-zinc-700">
                {file}
            </div>

            <Editor
                height="100%"
                language={language}
                value={code}
                onChange={(value) => setCode(value ?? "")}
                defaultValue={content}
                theme="vs-dark"
                onMount={handleEditorDidMount}
                options={{
                    automaticLayout: true,
                    minimap: {
                        enabled: false,
                    },
                    fontSize: 15,
                }}
            />

        </div>
    );
}