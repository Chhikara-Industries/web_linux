export interface TerminalEntry {

    path: string;

    command: string;

    output: string;

}


export interface TerminalResponse {

    mode:
        | "shell"
        | "editor";


    output: string;


    currentPath?: string;


    history?: TerminalEntry[];


    file?: string;


    language?: string;


    content?: string;


}