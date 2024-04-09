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

window.addEventListener('DOMContentLoaded', (event) => {
    document.getElementById('uploadBtn').addEventListener('click', loadCSVFiles);
    const headers = document.querySelectorAll('#data-table th');
    headers.forEach(header => {
        header.addEventListener('click', () => sortColumn(header.id));
    });
});

let sortDirections = {}; // Keeps track of the sort directions of columns

function sortColumn(columnId) {
    const tableBody = document.getElementById('data-table').getElementsByTagName('tbody')[0];
    const rowsArray = Array.from(tableBody.rows);
    const columnIndex = [...tableBody.parentNode.rows[0].cells].findIndex(cell => cell.id === columnId);

    // Determine sort direction
    const isAscending = sortDirections[columnId] !== 'asc';
    sortDirections[columnId] = isAscending ? 'asc' : 'desc';

    // Sort rows based on the content of the clicked column
    rowsArray.sort((rowA, rowB) => {
        const cellA = rowA.cells[columnIndex].innerText.toLowerCase();
        const cellB = rowB.cells[columnIndex].innerText.toLowerCase();

        // Adjust for numeric sorting if necessary
        if (columnId === 'duration' || columnId === 'tokensPerSecond') {
            return isAscending ? parseFloat(cellA) - parseFloat(cellB) : parseFloat(cellB) - parseFloat(cellA);
        } else {
            return isAscending ? cellA.localeCompare(cellB) : cellB.localeCompare(cellA);
        }
    });

    // Reattach rows to the table body in their new order
    rowsArray.forEach(row => tableBody.appendChild(row));

    // Update sort indicators (arrows or similar) on headers
    document.querySelectorAll('#data-table th').forEach(th => {
        if (th.id === columnId) {
            th.classList.remove('asc', 'desc');
            th.classList.add(sortDirections[columnId]);
        } else {
            th.classList.remove('asc', 'desc'); // Remove sort indicators from other headers
        }
    });
}

