"use client";

import { useState } from "react";
import dynamic from "next/dynamic";
import TerminalScreen from "@/components/TerminalScreen";
import PythonRepl from "@/components/PythonRepl";

const EditorScreen = dynamic(() => import("@/components/EditorScreen"), {
    ssr: false,
});

export default function Home() {
    const [mode, setMode] = useState<"shell" | "editor" | "python">("shell");
    const [path, setPath] = useState("~");

    const [editor, setEditor] = useState({
        file: "",
        language: "plaintext",
        content: "",
    });

    if (mode === "editor") {
        return (
            <EditorScreen
                file={editor.file}
                language={editor.language}
                content={editor.content}
                onExit={() => setMode("shell")}
            />
        );
    }

    if (mode === "python") {
        return (
            <PythonRepl
                onExit={() => setMode("shell")}
            />
        );
    }

    return (
        <TerminalScreen
            path={path}
            setPath={setPath}
            setMode={setMode}
            setEditor={setEditor}
        />
    );
}
