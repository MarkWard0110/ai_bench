window.addEventListener('DOMContentLoaded', (event) => {
    document.getElementById('uploadBtn').addEventListener('click', loadCSVFiles);
});

function loadCSVFiles() {
    const input = document.getElementById('csvFileInput');
    Array.from(input.files).forEach(file => {
        const reader = new FileReader();
        reader.onload = function(e) {
            const text = e.target.result;
            parseCSV(text);
        };
        reader.readAsText(file);
    });
}

function parseCSV(text) {
    const rows = text.split('\n');
    const tableBody = document.getElementById('data-table').getElementsByTagName('tbody')[0];

    rows.slice(1).forEach((row) => {
        if (row) {
            const cells = parseCSVRow(row);

            const tableRow = tableBody.insertRow();
            cells.forEach((cell, index) => {
                const cellElement = tableRow.insertCell();
                // Replace \n with HTML line breaks for display. Adjust index based on your CSV structure.
                cellElement.innerHTML = index >= 3 ? cell.replace(/\\n/g, '<br>') : cell;
            });
        }
    });
}

function parseCSVRow(row) {
    const cells = [];
    let inQuotes = false;
    let valueBuffer = '';

    // Loop through each character in the row
    for (let i = 0; i < row.length; i++) {
        const char = row[i];

        if (char === '"') {
            inQuotes = !inQuotes;
            continue;
        }

        if (char === ',' && !inQuotes) {
            // End of the cell value
            cells.push(valueBuffer);
            valueBuffer = '';
        } else {
            valueBuffer += char;
        }
    }
    // Add the last value
    if (valueBuffer.length > 0) {
        cells.push(valueBuffer);
    }

    return cells.map(cell => cell.trim().replace(/""/g, '"')); // Handle escaped quotes
}

// Add sorting functionality and keyboard navigation here
