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
        editor.addCommand(
            monaco.KeyMod.CtrlCmd | monaco.KeyCode.KeyQ,
            () => onExit()
        );

        editor.addCommand(
            monaco.KeyMod.CtrlCmd | monaco.KeyCode.KeyS,
            () => {
                console.log("Saving:", file);
            }
        );

        editor.focus();
    };

    return (
        <div className="h-full w-full flex flex-col bg-[#1e1e1e]">

            <div className="
                flex
                items-center
                justify-between
                px-3
                py-1.5
                bg-[#2d2d2d]
                border-b
                border-[#404040]
                text-sm
                text-neutral-400
                shrink-0
            ">
                <div className="flex items-center gap-2">
                    <span className="
                        w-3
                        h-3
                        rounded-full
                        bg-red-500
                    " />
                    <span className="
                        w-3
                        h-3
                        rounded-full
                        bg-yellow-500
                    " />
                    <span className="
                        w-3
                        h-3
                        rounded-full
                        bg-green-500
                    " />
                </div>

                <span className="text-neutral-300">
                    {file}
                </span>

                <span className="text-xs text-neutral-500">
                    Ctrl+Q to exit
                </span>
            </div>

            <div className="flex-1 min-h-0">
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
                        minimap: { enabled: false },
                        fontSize: 14,
                        fontFamily: "'Geist Mono', 'Cascadia Code', 'Fira Code', monospace",
                        padding: { top: 12 },
                        scrollBeyondLastLine: false,
                        renderLineHighlight: "gutter",
                        smoothScrolling: true,
                        cursorBlinking: "smooth",
                        cursorSmoothCaretAnimation: "on",
                    }}
                />
            </div>

        </div>
    );
}
