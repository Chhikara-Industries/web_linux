"use client";

import { useState, useRef, useEffect, Dispatch, SetStateAction } from "react";
import { TerminalEntry, TerminalResponse } from "@/lib/TerminalResponse";


type TerminalScreenProps = {
    path: string;
    setPath: Dispatch<SetStateAction<string>>;

    setMode: Dispatch<SetStateAction<"shell" | "editor" | "python">>;

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
    const [history, setHistory] = useState<TerminalEntry[]>([]);
    const [cmdHistory, setCmdHistory] = useState<string[]>([]);
    const [historyIndex, setHistoryIndex] = useState(-1);

    const scrollRef = useRef<HTMLDivElement>(null);
    const inputRef = useRef<HTMLInputElement>(null);


    useEffect(() => {
        scrollRef.current?.scrollTo(0, scrollRef.current.scrollHeight);
    }, [history]);


    useEffect(() => {
        inputRef.current?.focus();
    }, []);


    async function execute() {

        const trimmed = command.trim();
        if (trimmed === "") return;


        setCmdHistory(prev => [...prev, trimmed]);
        setHistoryIndex(-1);


        const response = await fetch(
            "http://localhost:5245/api/terminal",
            {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ Command: trimmed, Mode: "shell" })
            }
        );


        const data: TerminalResponse = await response.json();


        if (data.mode === "python") {
            setMode("python");
        }
        else if (data.mode === "editor") {
            setEditor({
                file: data.file ?? "",
                language: data.language ?? "plaintext",
                content: data.content ?? ""
            });
            setMode("editor");
        }
        else if (trimmed === "clear") {
            setHistory([]);
            setCmdHistory([]);
            setHistoryIndex(-1);
        }
        else {
            setHistory(data.history ?? []);
        }


        if (data.currentPath) {
            setPath(data.currentPath);
        }

        setCommand("");
    }


    function handleKeyDown(e: React.KeyboardEvent) {

        if (e.key === "Enter") {
            execute();
            return;
        }


        if (e.key === "ArrowUp") {
            e.preventDefault();
            if (cmdHistory.length === 0) return;

            const newIndex = historyIndex === -1
                ? cmdHistory.length - 1
                : Math.max(0, historyIndex - 1);

            setHistoryIndex(newIndex);
            setCommand(cmdHistory[newIndex]);
            return;
        }


        if (e.key === "ArrowDown") {
            e.preventDefault();
            if (historyIndex === -1) return;

            const newIndex = historyIndex + 1;
            if (newIndex >= cmdHistory.length) {
                setHistoryIndex(-1);
                setCommand("");
            } else {
                setHistoryIndex(newIndex);
                setCommand(cmdHistory[newIndex]);
            }
            return;
        }


        if (e.key === "l" && e.ctrlKey) {
            e.preventDefault();
            setHistory([]);
            return;
        }
    }


    return (
        <div
            style={{ padding: "20px 15px" }}
            className="h-full w-full overflow-y-auto cursor-text"
            ref={scrollRef}
            onClick={() => inputRef.current?.focus()}
        >

            {history.map((entry, i) => (
                <div key={i} className="leading-relaxed">
                    <div>
                        <span className="text-cyan-700 shrink-0">{"user@WebLinux "}</span>
                        <span className="text-emerald-400">
                            {entry.path}
                        </span>
                        <span className="text-amber-400">{" $ "}</span>
                        <span>{entry.command}</span>
                    </div>
                    {entry.output && (
                        <pre className="text-neutral-300 whitespace-pre-wrap">
                            {entry.output}
                        </pre>
                    )}
                </div>
            ))}


            <p><span className="text-cyan-700 shrink-0">{"user@WebLinux "}</span><span className="text-emerald-400 shrink-0 pt-[20px]">{path}</span> <span className="text-amber-400 shrink-0">{" $ "}</span> <input ref={inputRef} className="flex-1 bg-transparent outline-none text-white caret-amber-400 ml-0" autoFocus autoComplete="off" autoCorrect="off" autoCapitalize="off" spellCheck={false} value={command} onChange={(e) => setCommand(e.target.value)} onKeyDown={handleKeyDown}/></p>

        </div>
    );
}
