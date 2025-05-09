/**
 * PDF Export Utility
 * A utility for generating high-quality PDF exports from web content
 * For use with jsPDF and html2canvas
 */

const PdfExportUtility = {
    /**
     * Generate a PDF report
     * @param {Object} options - Configuration options for the PDF export
     * @param {HTMLElement} options.content - The content element that contains the data
     * @param {string} options.filename - The filename for the PDF
     * @param {string} options.title - The title for the PDF report
     * @param {string} options.orientation - The orientation of the PDF ('portrait' or 'landscape')
     * @param {Function} options.beforeExport - Callback function to run before export
     * @param {Function} options.afterExport - Callback function to run after export
     * @param {Function} options.onError - Callback function to run on error
     * @returns {Promise} - A promise that resolves when the PDF is generated
     */
    generatePdf: async function(options) {
        const defaults = {
            content: document.querySelector('.container'),
            filename: `report-${new Date().toISOString().slice(0,10)}.pdf`,
            title: 'Report',
            orientation: 'portrait',
            beforeExport: () => {},
            afterExport: () => {},
            onError: (err) => console.error('PDF generation error:', err)
        };

        const settings = { ...defaults, ...options };
        
        try {
            // Run before-export callback
            if (typeof settings.beforeExport === 'function') {
                settings.beforeExport();
            }
            
            // Create a dedicated container for the report that won't be affected by page styles
            const reportContainer = document.createElement('div');
            Object.assign(reportContainer.style, {
                position: 'absolute',
                left: '-9999px',
                top: '0',
                width: '210mm', // A4 width
                backgroundColor: 'white',
                fontFamily: 'Arial, Helvetica, sans-serif',
                fontSize: '12px',
                color: '#333',
                padding: '15mm',
                boxSizing: 'border-box',
                zIndex: '-1000'
            });
            document.body.appendChild(reportContainer);
            
            // Add report header with title and date
            const headerDiv = document.createElement('div');
            headerDiv.innerHTML = `
                <div style="text-align: center; margin-bottom: 20px;">
                    <h1 style="font-size: 24px; color: #333; margin: 0 0 10px 0;">${settings.title}</h1>
                    <p style="color: #666; margin: 0;">Generated: ${new Date().toLocaleString()}</p>
                    <div style="border-bottom: 2px solid #ddd; margin: 10px 0;"></div>
                </div>
            `;
            reportContainer.appendChild(headerDiv);
            
            // Function to extract card data
            const extractCardData = () => {
                const cardsData = [];
                const cardElements = settings.content.querySelectorAll('#summaryCards .card');
                cardElements.forEach(card => {
                    const title = card.querySelector('h5')?.textContent || '';
                    const value = card.querySelector('h2')?.textContent || '';
                    if (title && value) {
                        cardsData.push({ title, value });
                    }
                });
                return cardsData;
            };
            
            // Create the summary cards section
            const cardsData = extractCardData();
            if (cardsData.length > 0) {
                const cardsDiv = document.createElement('div');
                cardsDiv.style.display = 'flex';
                cardsDiv.style.flexWrap = 'wrap';
                cardsDiv.style.gap = '10px';
                cardsDiv.style.justifyContent = 'space-between';
                cardsDiv.style.marginBottom = '20px';
                
                cardsData.forEach(card => {
                    const cardDiv = document.createElement('div');
                    Object.assign(cardDiv.style, {
                        flex: '1',
                        minWidth: '120px',
                        padding: '10px',
                        border: '1px solid #ddd',
                        borderRadius: '5px',
                        textAlign: 'center',
                        backgroundColor: '#f9f9f9'
                    });
                    
                    cardDiv.innerHTML = `
                        <div style="font-size: 14px; color: #666; margin-bottom: 5px;">${card.title}</div>
                        <div style="font-size: 18px; font-weight: bold; color: #333;">${card.value}</div>
                    `;
                    
                    cardsDiv.appendChild(cardDiv);
                });
                
                reportContainer.appendChild(cardsDiv);
            }
            
            // Get chart title
            const chartTitle = settings.content.querySelector('#chartTitle')?.textContent || 'Chart';
            
            // Create chart section title
            const chartTitleDiv = document.createElement('div');
            chartTitleDiv.innerHTML = `
                <h2 style="font-size: 18px; margin: 20px 0 10px 0; padding-bottom: 5px; border-bottom: 1px solid #eee;">${chartTitle}</h2>
            `;
            reportContainer.appendChild(chartTitleDiv);
            
            // Find and capture the chart
            const chartCanvas = settings.content.querySelector('#mainChart');
            if (chartCanvas) {
                try {
                    // First capture the chart using html2canvas for better quality
                    const chartImage = await html2canvas(chartCanvas, {
                        scale: 2,
                        useCORS: true,
                        allowTaint: true,
                        backgroundColor: 'white'
                    });
                    
                    // Create an image element with the chart data
                    const chartDiv = document.createElement('div');
                    chartDiv.style.textAlign = 'center';
                    chartDiv.style.marginBottom = '20px';
                    
                    const img = document.createElement('img');
                    img.src = chartImage.toDataURL('image/png');
                    img.style.maxWidth = '100%';
                    img.style.height = 'auto';
                    img.style.maxHeight = '300px';
                    img.style.border = '1px solid #eee';
                    img.style.borderRadius = '5px';
                    img.style.padding = '10px';
                    
                    chartDiv.appendChild(img);
                    reportContainer.appendChild(chartDiv);
                } catch (err) {
                    console.warn('Error capturing chart:', err);
                    // Add a placeholder if chart capture fails
                    const errorDiv = document.createElement('div');
                    errorDiv.style.textAlign = 'center';
                    errorDiv.style.padding = '20px';
                    errorDiv.style.backgroundColor = '#f8f8f8';
                    errorDiv.style.border = '1px solid #ddd';
                    errorDiv.style.borderRadius = '5px';
                    errorDiv.style.marginBottom = '20px';
                    errorDiv.textContent = 'Chart could not be displayed';
                    reportContainer.appendChild(errorDiv);
                }
            }
            
            // Get table data
            const dataTable = settings.content.querySelector('.table');
            if (dataTable) {
                // Get table title
                const tableTitle = dataTable.closest('.card')?.querySelector('.card-header h5')?.textContent || 'Detailed Data';
                
                // Create table section title
                const tableTitleDiv = document.createElement('div');
                tableTitleDiv.innerHTML = `
                    <h2 style="font-size: 18px; margin: 20px 0 10px 0; padding-bottom: 5px; border-bottom: 1px solid #eee;">${tableTitle}</h2>
                `;
                reportContainer.appendChild(tableTitleDiv);
                
                // Create a new table with better styling for PDF
                const table = document.createElement('table');
                Object.assign(table.style, {
                    width: '100%',
                    borderCollapse: 'collapse',
                    marginBottom: '20px',
                    fontSize: '10px' // Smaller font for tables to fit more data
                });
                
                // Create table header
                const thead = document.createElement('thead');
                const headerRow = document.createElement('tr');
                
                const headerCells = dataTable.querySelectorAll('th');
                headerCells.forEach(cell => {
                    const th = document.createElement('th');
                    th.textContent = cell.textContent.replace(/[^\w\s]/gi, '').trim(); // Remove icons and extra whitespace
                    Object.assign(th.style, {
                        backgroundColor: '#f2f2f2',
                        padding: '8px',
                        textAlign: 'left',
                        fontWeight: 'bold',
                        border: '1px solid #ddd'
                    });
                    headerRow.appendChild(th);
                });
                
                thead.appendChild(headerRow);
                table.appendChild(thead);
                
                // Create table body with data
                const tbody = document.createElement('tbody');
                const rows = dataTable.querySelectorAll('tbody tr');
                
                // Limit rows to keep PDF manageable
                const maxRows = Math.min(rows.length, 15);
                
                for (let i = 0; i < maxRows; i++) {
                    const row = rows[i];
                    const tr = document.createElement('tr');
                    tr.style.backgroundColor = i % 2 === 0 ? '#ffffff' : '#f9f9f9';
                    
                    const cells = row.querySelectorAll('td');
                    cells.forEach(cell => {
                        const td = document.createElement('td');
                        
                        // Handle progress bars or other special content
                        if (cell.querySelector('.progress')) {
                            // For progress bars, just use the text value
                            td.textContent = cell.textContent.trim();
                        } else {
                            td.textContent = cell.textContent.trim();
                        }
                        
                        Object.assign(td.style, {
                            padding: '5px',
                            textAlign: 'left',
                            border: '1px solid #ddd',
                            verticalAlign: 'top'
                        });
                        
                        tr.appendChild(td);
                    });
                    
                    tbody.appendChild(tr);
                }
                
                // Add indicator for truncated data
                if (rows.length > maxRows) {
                    const tr = document.createElement('tr');
                    const td = document.createElement('td');
                    td.colSpan = headerCells.length;
                    td.textContent = `... and ${rows.length - maxRows} more items not shown`;
                    Object.assign(td.style, {
                        padding: '5px',
                        textAlign: 'center',
                        border: '1px solid #ddd',
                        backgroundColor: '#f2f2f2',
                        fontStyle: 'italic'
                    });
                    
                    tr.appendChild(td);
                    tbody.appendChild(tr);
                }
                
                table.appendChild(tbody);
                reportContainer.appendChild(table);
            }
            
            // Add footer
            const footerDiv = document.createElement('div');
            Object.assign(footerDiv.style, {
                marginTop: '30px',
                borderTop: '1px solid #eee',
                paddingTop: '10px',
                fontSize: '10px',
                color: '#666',
                textAlign: 'center'
            });
            
            footerDiv.innerHTML = `
                <p style="margin: 0 0 5px 0;">This report was automatically generated by the Food Ordering System.</p>
                <p style="margin: 0;">© ${new Date().getFullYear()} Food Ordering System</p>
            `;
            reportContainer.appendChild(footerDiv);
            
            // Use html2canvas and jsPDF for highest quality output
            const pdfCanvas = await html2canvas(reportContainer, {
                scale: 2, // Higher scale for better quality
                useCORS: true,
                allowTaint: true,
                logging: false,
                backgroundColor: 'white'
            });
            
            // Create PDF with jsPDF
            const imgData = pdfCanvas.toDataURL('image/jpeg', 1.0);
            const pdf = new jspdf.jsPDF({
                orientation: settings.orientation,
                unit: 'mm',
                format: 'a4'
            });
            
            const pdfWidth = pdf.internal.pageSize.getWidth();
            const pdfHeight = pdf.internal.pageSize.getHeight();
            const imgWidth = pdfWidth;
            const imgHeight = (pdfCanvas.height * imgWidth) / pdfCanvas.width;
            
            // Add image across multiple pages if needed
            let heightLeft = imgHeight;
            let position = 0;
            
            // First page
            pdf.addImage(imgData, 'JPEG', 0, position, imgWidth, imgHeight);
            heightLeft -= pdfHeight;
            
            // Additional pages if needed
            while (heightLeft > 0) {
                position = heightLeft - imgHeight; // negative value
                pdf.addPage();
                pdf.addImage(imgData, 'JPEG', 0, position, imgWidth, imgHeight);
                heightLeft -= pdfHeight;
            }
            
            // Save the PDF
            pdf.save(settings.filename);
            
            // Clean up - remove the temporary container
            if (reportContainer && reportContainer.parentNode) {
                reportContainer.parentNode.removeChild(reportContainer);
            }
            
            // Run after-export callback
            if (typeof settings.afterExport === 'function') {
                settings.afterExport();
            }
            
            return true;
        } catch (error) {
            console.error('PDF generation error:', error);
            
            // Run error callback
            if (typeof settings.onError === 'function') {
                settings.onError(error);
            }
            
            return false;
        }
    }
}; 