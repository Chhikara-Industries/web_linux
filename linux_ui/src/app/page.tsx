"use client";

import { useState } from "react";
import TerminalScreen from "@/components/TerminalScreen";
import EditorScreen from "@/components/EditorScreen";

export default function Home() {
    const [mode, setMode] = useState<"shell" | "editor">("shell");
    const [path, setPath] = useState("~");
    
    const [editor, setEditor] = useState({
      file: "",
      language: "plaintext",
      content: "",
    });


    return (
        <>
            {mode === "shell" ? (
                <TerminalScreen
                    path={path}
                    setPath={setPath}
                    setMode={setMode}
                    setEditor={setEditor}
                />
            ) : (
                <EditorScreen
                    file={editor.file}
                    language={editor.language}
                    content={editor.content}
                    onExit={()=>setMode("shell")}
                />
            )}
        </>
    );
}