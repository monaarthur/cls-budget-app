export type ExportCell = string | number | boolean | null | undefined;

export type ExportColumn<T> = {
  header: string;
  value: (row: T) => ExportCell;
  kind?: "text" | "number" | "boolean";
  /** Preferred relative width for PDF columns. */
  width?: number;
};

function stamp(): string {
  const d = new Date();
  const yyyy = d.getFullYear();
  const mm = String(d.getMonth() + 1).padStart(2, "0");
  const dd = String(d.getDate()).padStart(2, "0");
  return `${yyyy}-${mm}-${dd}`;
}

function downloadBlob(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.setTimeout(() => URL.revokeObjectURL(url), 1_000);
}

function cellText(value: ExportCell): string {
  if (value == null) return "";
  if (typeof value === "boolean") return value ? "Yes" : "No";
  return String(value);
}

function escapeXml(value: string): string {
  return value
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}

function excelCell(value: ExportCell, kind: ExportColumn<unknown>["kind"]): string {
  if (kind === "number") {
    const n = typeof value === "number" ? value : Number(value);
    if (value == null || value === "" || !Number.isFinite(n)) {
      return `<Cell><Data ss:Type="String"></Data></Cell>`;
    }
    return `<Cell><Data ss:Type="Number">${n}</Data></Cell>`;
  }

  const text = escapeXml(cellText(value));
  return `<Cell><Data ss:Type="String">${text}</Data></Cell>`;
}

export function downloadExcelTable<T>(
  title: string,
  columns: ExportColumn<T>[],
  rows: T[],
  filenameBase: string,
): void {
  const header = columns
    .map(
      (col) =>
        `<Cell ss:StyleID="Header"><Data ss:Type="String">${escapeXml(col.header)}</Data></Cell>`,
    )
    .join("");

  const body = rows
    .map((row) => {
      const cells = columns
        .map((col) => excelCell(col.value(row), col.kind))
        .join("");
      return `<Row>${cells}</Row>`;
    })
    .join("");

  const xml = `<?xml version="1.0"?>
<?mso-application progid="Excel.Sheet"?>
<Workbook xmlns="urn:schemas-microsoft-com:office:spreadsheet"
 xmlns:ss="urn:schemas-microsoft-com:office:spreadsheet">
  <Styles>
    <Style ss:ID="Header">
      <Font ss:Bold="1"/>
    </Style>
  </Styles>
  <Worksheet ss:Name="${escapeXml(title).slice(0, 31) || "Sheet1"}">
    <Table>
      <Row>${header}</Row>
      ${body}
    </Table>
  </Worksheet>
</Workbook>`;

  downloadBlob(
    new Blob([xml], { type: "application/vnd.ms-excel" }),
    `${filenameBase}-${stamp()}.xls`,
  );
}

function pdfEscape(value: string): string {
  return value
    .normalize("NFKD")
    .replace(/[^\x20-\x7E]/g, "?")
    .replace(/\\/g, "\\\\")
    .replace(/\(/g, "\\(")
    .replace(/\)/g, "\\)");
}

function truncate(value: string, max: number): string {
  if (value.length <= max) return value;
  return `${value.slice(0, Math.max(0, max - 1))}…`;
}

/**
 * Landscape letter PDF table using built-in Helvetica (no extra packages).
 */
export function downloadPdfTable<T>(
  title: string,
  columns: ExportColumn<T>[],
  rows: T[],
  filenameBase: string,
): void {
  const pageWidth = 792;
  const pageHeight = 612;
  const margin = 36;
  const tableWidth = pageWidth - margin * 2;
  const rowHeight = 16;
  const fontSize = 8;
  const headerSize = 14;

  const weightSum = columns.reduce((sum, col) => sum + (col.width ?? 1), 0);
  const colWidths = columns.map(
    (col) => ((col.width ?? 1) / weightSum) * tableWidth,
  );

  const objects: string[] = [];
  const pages: number[] = [];

  const addObject = (body: string): number => {
    objects.push(body);
    return objects.length;
  };

  let content = "";
  let y = pageHeight - margin;

  const startPage = () => {
    content = "BT\n";
    y = pageHeight - margin;
    content += `/F1 ${headerSize} Tf\n`;
    content += `1 0 0 1 ${margin} ${y - 12} Tm (${pdfEscape(title)}) Tj\n`;
    y -= 22;
    content += `/F1 ${fontSize} Tf\n`;
    drawRow(
      columns.map((col) => col.header),
      true,
    );
  };

  const finishPage = () => {
    content += "ET\n";
    const stream = `<< /Length ${content.length} >>\nstream\n${content}endstream`;
    const contentId = addObject(stream);
    const pageId = addObject(
      `<< /Type /Page /Parent PAGES /MediaBox [0 0 ${pageWidth} ${pageHeight}] /Resources << /Font << /F1 FONT >> >> /Contents ${contentId} 0 R >>`,
    );
    pages.push(pageId);
  };

  const drawRow = (values: string[], header: boolean) => {
    if (y - rowHeight < margin) {
      finishPage();
      startPage();
    }

    let x = margin;
    values.forEach((value, index) => {
      const width = colWidths[index] ?? 40;
      const maxChars = Math.max(4, Math.floor(width / 4.4));
      const shown = truncate(value, maxChars);
      if (header) {
        content += `/F1 ${fontSize} Tf\n`;
      }
      content += `1 0 0 1 ${x + 2} ${y - 11} Tm (${pdfEscape(shown)}) Tj\n`;
      x += width;
    });
    y -= rowHeight;
  };

  startPage();
  for (const row of rows) {
    drawRow(
      columns.map((col) => cellText(col.value(row))),
      false,
    );
  }
  finishPage();

  const fontId = addObject(
    "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
  );
  const kids = pages.map((id) => `${id} 0 R`).join(" ");
  const pagesId = addObject(
    `<< /Type /Pages /Kids [ ${kids} ] /Count ${pages.length} >>`,
  );
  const catalogId = addObject(
    `<< /Type /Catalog /Pages ${pagesId} 0 R >>`,
  );

  const resolved = objects.map((body) =>
    body
      .replace(/\bPAGES\b/g, `${pagesId} 0 R`)
      .replace(/\bFONT\b/g, `${fontId} 0 R`),
  );

  let pdf = "%PDF-1.4\n";
  const offsets = [0];
  for (let i = 0; i < resolved.length; i++) {
    offsets.push(pdf.length);
    pdf += `${i + 1} 0 obj\n${resolved[i]}\nendobj\n`;
  }
  const xref = pdf.length;
  pdf += `xref\n0 ${resolved.length + 1}\n`;
  pdf += "0000000000 65535 f \n";
  for (let i = 1; i < offsets.length; i++) {
    pdf += `${String(offsets[i]).padStart(10, "0")} 00000 n \n`;
  }
  pdf += `trailer\n<< /Size ${resolved.length + 1} /Root ${catalogId} 0 R >>\n`;
  pdf += `startxref\n${xref}\n%%EOF`;

  downloadBlob(new Blob([pdf], { type: "application/pdf" }), `${filenameBase}-${stamp()}.pdf`);
}
