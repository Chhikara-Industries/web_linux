"use client";

import { useState, useRef, useEffect } from "react";
import { TerminalResponse } from "@/lib/TerminalResponse";

type ReplEntry = {
    input: string;
    output: string;
};

type PythonReplProps = {
    onExit: () => void;
};

const API = "http://localhost:5245/api/terminal";

export default function PythonRepl({ onExit }: PythonReplProps) {
    const [entries, setEntries] = useState<ReplEntry[]>([]);
    const [input, setInput] = useState("");
    const [loading, setLoading] = useState(true);

    const scrollRef = useRef<HTMLDivElement>(null);
    const inputRef = useRef<HTMLInputElement>(null);

    useEffect(() => {
        scrollRef.current?.scrollTo(0, scrollRef.current.scrollHeight);
    }, [entries]);

    useEffect(() => {
        inputRef.current?.focus();
    }, []);

    useEffect(() => {
        let cancelled = false;

        async function init() {
            try {
                const res = await fetch(API, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ Command: "", Mode: "python" }),
                });
                const data: TerminalResponse = await res.json();
                if (cancelled) return;

                if (data.mode === "shell") {
                    onExit();
                    return;
                }

                if (data.output) {
                    setEntries([{ input: "", output: data.output }]);
                }
            } catch {
                if (!cancelled) {
                    setEntries([{ input: "", output: "Failed to connect to Python backend." }]);
                }
            } finally {
                if (!cancelled) setLoading(false);
            }
        }

        init();
        return () => { cancelled = true; };
    }, [onExit]);


    async function handleSubmit(e: React.KeyboardEvent) {
        if (e.key === "Enter" && !e.shiftKey) {
            e.preventDefault();
        } else {
            return;
        }

        const trimmed = input.trim();
        setInput("");

        if (trimmed === "") {
            setEntries((prev) => [...prev, { input: "", output: "" }]);
        }

        setEntries((prev) => [...prev, { input: trimmed, output: "" }]);

        try {
            const res = await fetch(API, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ Command: trimmed, Mode: "python" }),
            });
            const data: TerminalResponse = await res.json();

            if (data.mode === "shell") {
                onExit();
                return;
            }

            setEntries((prev) => {
                const updated = [...prev];
                updated[updated.length - 1] = {
                    input: trimmed,
                    output: data.output ?? "",
                };
                return updated;
            });
        } catch {
            setEntries((prev) => {
                const updated = [...prev];
                updated[updated.length - 1] = {
                    input: trimmed,
                    output: "Error: failed to reach backend.",
                };
                return updated;
            });
        }
    }


    if (loading) {
        return (
            <div
                style={{ padding: "20px 15px" }}
                className="h-full w-full overflow-y-auto cursor-text"
            >
                <div className="text-emerald-400 mb-2">Python</div>
                <div className="text-neutral-400">Connecting to Python runtime...</div>
            </div>
        );
    }

    return (
        <div
            style={{ padding: "20px 15px" }}
            className="h-full w-full overflow-y-auto cursor-text"
            ref={scrollRef}
            onClick={() => inputRef.current?.focus()}
        >
            <div className="text-emerald-400 mb-2">
                Python — Type &quot;exit()&quot; to return to shell
            </div>

            {entries.map((entry, i) => (
                <div key={i} className="leading-relaxed">
                    <div>
                        <span className="text-amber-400">
                            {entry.input === "" ? "" : ">>> "}
                        </span>
                        <span>{entry.input}</span>
                    </div>
                    {entry.output && (
                        <pre className="text-neutral-300 whitespace-pre-wrap">
                            {entry.output}
                        </pre>
                    )}
                </div>
            ))}

            <div className="flex items-center leading-relaxed">
                <span className="text-amber-400 shrink-0">{">>> "}</span>
                <input
                    ref={inputRef}
                    className="
                        flex-1
                        bg-transparent
                        outline-none
                        text-white
                        caret-amber-400
                    "
                    autoFocus
                    autoComplete="off"
                    autoCorrect="off"
                    autoCapitalize="off"
                    spellCheck={false}
                    value={input}
                    onChange={(e) => setInput(e.target.value)}
                    onKeyDown={handleSubmit}
                />
            </div>
        </div>
    );
}
