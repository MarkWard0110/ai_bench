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
    const lines = text.split('\n');
    const tableBody = document.getElementById('data-table').getElementsByTagName('tbody')[0];

    lines.slice(1).forEach(line => {
        if (line) {
            const row = tableBody.insertRow();
            const cells = parseCSVRow(line);

            cells.forEach((cell, index) => {
                const cellElement = row.insertCell();
                if (index === 0) { // Assuming the Model column is the first column
                    cellElement.classList.add('no-wrap'); // Add class to prevent text wrapping
                }
                if (index === 3 || index === 4) { // Handle special formatting for Prompt and Response
                    const preElement = document.createElement('pre');
                    preElement.textContent = cell.replace(/\\n/g, '\n');
                    cellElement.appendChild(preElement);
                } else {
                    cellElement.textContent = cell;
                }
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
        let cellA = rowA.cells[columnIndex].textContent.toLowerCase();
        let cellB = rowB.cells[columnIndex].textContent.toLowerCase();

        // Convert to appropriate format for comparison based on column
        if (columnId === 'duration') {
            cellA = durationToMilliseconds(cellA);
            cellB = durationToMilliseconds(cellB);
            return isAscending ? cellA - cellB : cellB - cellA;
        } else if (columnId === 'tokensPerSecond') {
            cellA = parseFloat(cellA);
            cellB = parseFloat(cellB);
            return isAscending ? cellA - cellB : cellB - cellA;
        } else {
            // Use localeCompare for string comparison
            return isAscending ? cellA.localeCompare(cellB) : cellB.localeCompare(cellA);
        }
    });

    // Reattach rows to the table body in their new order
    rowsArray.forEach(row => tableBody.appendChild(row));

    // Update sort indicators on headers
    document.querySelectorAll('#data-table th').forEach(th => {
        if (th.id === columnId) {
            th.classList.remove('asc', 'desc');
            th.classList.add(sortDirections[columnId]);
        } else {
            th.classList.remove('asc', 'desc');
        }
    });
}


function durationToMilliseconds(duration) {
    const parts = duration.split(":");
    const hours = parseInt(parts[0], 10);
    const minutes = parseInt(parts[1], 10);
    const seconds = parseFloat(parts[2]);
    return (hours * 3600 + minutes * 60 + seconds) * 1000;
}


document.getElementById('modelFilterInput').addEventListener('input', function() {
    filterTableByModel(this.value.toLowerCase());
});

function filterTableByModel(filterValue) {
    const tableBody = document.getElementById('data-table').getElementsByTagName('tbody')[0];
    const rows = tableBody.rows;

    // Loop through all table rows, and hide those that don't match the search query
    for (let i = 0; i < rows.length; i++) {
        let modelCell = rows[i].getElementsByTagName("td")[0]; // Assuming the Model is the first column
        if (modelCell) {
            let modelText = modelCell.textContent || modelCell.innerText;
            if (modelText.toLowerCase().indexOf(filterValue) > -1) {
                rows[i].style.display = ""; // The row matches the filter; show it
            } else {
                rows[i].style.display = "none"; // The row does not match the filter; hide it
            }
        }
    }
}
