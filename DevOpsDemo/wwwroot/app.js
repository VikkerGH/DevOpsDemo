"use strict";

const form = document.querySelector("#book-form");
const titleInput = document.querySelector("#title");
const authorInput = document.querySelector("#author");
const isReadInput = document.querySelector("#is-read");
const statusElement = document.querySelector("#status");
const tableBody = document.querySelector("#books");
const cancelButton = document.querySelector("#cancel");
let editingId = null;

function showStatus(message, isError = false) {
    statusElement.textContent = message;
    statusElement.dataset.error = String(isError);
}

// API'et ligger på samme adresse som HTML-siden: ingen hardkodet localhost-URL.
async function api(path, method = "GET", body) {
    const options = { method };
    if (body !== undefined) {
        options.headers = { "Content-Type": "application/json" };
        options.body = JSON.stringify(body);
    }
    const response = await fetch(`/api/books${path}`, options);
    if (!response.ok) {
        const problem = await response.json();
        const message = problem.errors
            ? Object.values(problem.errors).flat().join(" ")
            : problem.detail || problem.title || `HTTP-fejl ${response.status}`;
        throw new Error(message);
    }
    // DELETE returnerer 204 uden indhold; der er derfor ingen JSON at parse.
    return response.status === 204 ? null : response.json();
}

function resetForm() {
    editingId = null;
    form.reset();
    document.querySelector("#form-heading").textContent = "Tilføj en bog";
    document.querySelector("#save").textContent = "Gem bog";
    cancelButton.hidden = true;
}

function editBook(book) {
    editingId = book.id;
    titleInput.value = book.title;
    authorInput.value = book.author;
    isReadInput.checked = book.isRead;
    document.querySelector("#form-heading").textContent = `Rediger bog #${book.id}`;
    document.querySelector("#save").textContent = "Gem rettelser";
    cancelButton.hidden = false;
    titleInput.focus();
}

async function loadBooks() {
    const books = await api("");
    tableBody.replaceChildren();
    for (const book of books) {
        const row = document.createElement("tr");
        for (const value of [book.id, book.title, book.author, book.isRead ? "Læst" : "Ikke læst"]) {
            const cell = document.createElement("td");
            // textContent viser brugerinput som tekst, aldrig som HTML.
            cell.textContent = String(value);
            row.append(cell);
        }
        const actions = document.createElement("td");
        const editButton = document.createElement("button");
        editButton.type = "button";
        editButton.className = "secondary";
        editButton.textContent = "Rediger";
        editButton.setAttribute("aria-label", `Rediger ${book.title}`);
        editButton.addEventListener("click", () => editBook(book));
        const deleteButton = document.createElement("button");
        deleteButton.type = "button";
        deleteButton.className = "danger";
        deleteButton.textContent = "Slet";
        deleteButton.setAttribute("aria-label", `Slet ${book.title}`);
        deleteButton.addEventListener("click", () => {
            if (confirm(`Vil du slette "${book.title}"?`)) {
                runAction(async () => {
                    await api(`/${book.id}`, "DELETE");
                    if (editingId === book.id) resetForm();
                    await loadBooks();
                    showStatus("Bogen er slettet.");
                });
            }
        });
        actions.append(editButton, deleteButton);
        row.append(actions);
        tableBody.append(row);
    }
    showStatus(books.length === 0 ? "Kataloget er tomt. Tilføj din første bog." : `${books.length} bog/bøger i kataloget.`);
}

async function runAction(action) {
    // Deaktivering forhindrer dobbeltklik, mens et HTTP-kald er i gang.
    document.querySelector("#book-fields").disabled = true;
    document.querySelectorAll("button").forEach(button => { button.disabled = true; });
    try {
        await action();
    } catch (error) {
        showStatus(error instanceof Error ? error.message : "En ukendt fejl opstod.", true);
    } finally {
        document.querySelector("#book-fields").disabled = false;
        document.querySelectorAll("button").forEach(button => { button.disabled = false; });
    }
}

form.addEventListener("submit", event => {
    event.preventDefault(); // Send JSON til REST API'et i stedet for at genindlæse siden.
    runAction(async () => {
        const book = { title: titleInput.value.trim(), author: authorInput.value.trim(), isRead: isReadInput.checked };
        const isNew = editingId === null;
        await api(isNew ? "" : `/${editingId}`, isNew ? "POST" : "PUT", book);
        resetForm();
        await loadBooks();
        showStatus(isNew ? "Bogen er oprettet." : "Bogen er opdateret.");
    });
});
cancelButton.addEventListener("click", resetForm);
document.querySelector("#reload").addEventListener("click", () => runAction(loadBooks));
runAction(loadBooks);
