import { CodeJar } from 'https://cdn.jsdelivr.net/npm/codejar@3.3.0/codejar.min.js';
import { withLineNumbers } from 'https://cdn.jsdelivr.net/npm/codejar@3.3.0/linenumbers.min.js';

function tokenUnderCursor(event) {
    const caretPosition = document.caretPositionFromPoint(event.clientX, event.clientY);
    const text = caretPosition.offsetNode.textContent
    const start = caretPosition.offset;
    const before = text.slice(0, start);
    const preTokenOffset = before.search(/(^|[^A-Za-z0-9_])[A-Za-z0-9_]+$/);
    if (preTokenOffset < 0) return "nothing before";
    const after = text.slice(start);
    let postTokenOffset = after.search(/[^A-Za-z0-9_]/);
    if (postTokenOffset < 0) postTokenOffset = after.length;
    return before.slice(preTokenOffset) + after.slice(0, postTokenOffset);
}

function gotoDefinition(name) {
    const pattern = "(^|\\n|\\r)" + name + "[^A-Za-z0-9_]";
    const content = editor.textContent;
    let start = content.search(pattern);
    if (start > 0) start++;
    if (start >= 0) {
        editor.focus();
        editor.textContent = content.substring(0, start + 1);
        editor.scrollTop = start;
        const realScrollTop = editor.scrollTop;
        editor.textContent = content;
        highlight(editor);
        jar.restore({ start: start, end: start });
        if (realScrollTop < editor.clientHeight)
            editor.scrollTop = realScrollTop;
        else
            editor.scrollTop = realScrollTop + editor.clientHeight / 2;
    }
    else
        console.log("Can't find: " + name);
}

window.addEventListener('message', function (event) {
    if (event.source == window.opener) {
        var message = event.data;
        if (typeof message === "string") {
            jar.updateCode(DotNet.invokeMethod('StepWasmStandalone', 'SetSource', message));
            dirty = false;
        } else if (typeof message === "object" && "packageRepository" in message)
            packageRepository = message.packageRepository;
    } else
        console.log("wrong message sender");
});

window.addEventListener("beforeunload", function (event) {
    if (dirty) {
        event.preventDefault();
        event.returnValue = '';
    }
});

function installCodeJar() {
    const highlight = editor => {
        // highlight.js does not trims old tags,
        // let's do it by this hack.
        editor.textContent = editor.textContent;
        delete editor.dataset.highlighted;
        hljs.highlightElement(editor);
    };

    const editor = document.querySelector(".language-step");
    const jar = CodeJar(editor, withLineNumbers(highlight));

    // Put jar someplace the non-module scripts can find it.
    window.jar = jar;

    let dirty = false;

    editor.addEventListener("click", function (event) {
        if (!event.ctrlKey)
            return;
        event.preventDefault();
        const name = tokenUnderCursor(event);
        if (/^[A-Z][A-Za-z0-9_]*$/.test(name))
            gotoDefinition(name);
        else
            console.log("Token is not a name: " + name);
    });

    const downloadButton = document.getElementById('downloadButton');

    if (downloadButton != null)
        downloadButton.addEventListener('click', function () {
            // Create a Blob object with the content
            const blob = new Blob([jar.toString()], { type: "text/plain" });

            // Create a temporary anchor element
            const a = document.createElement("a");
            a.href = URL.createObjectURL(blob);
            a.download = "Saved.step"; // Set the file name

            // Append the anchor to the document, trigger the download, and remove it
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
        });
}

window.installCodeJar = installCodeJar;