/** Renders internal documentation markdown for the admin viewer (trusted repo files). */

export function renderInternalMarkdown(markdown: string): string {
  const normalized = markdown.replace(/\r\n/g, '\n');
  const lines = normalized.split('\n');
  const html: string[] = [];
  let i = 0;

  while (i < lines.length) {
    const line = lines[i] ?? '';

    if (line.startsWith('```')) {
      const fence: string[] = [];
      i += 1;
      while (i < lines.length && !(lines[i] ?? '').startsWith('```')) {
        fence.push(lines[i] ?? '');
        i += 1;
      }
      html.push(`<pre><code>${escapeHtml(fence.join('\n'))}</code></pre>`);
      if (i < lines.length) {
        i += 1;
      }
      continue;
    }

    if (/^\s*\|.+\|\s*$/.test(line) && i + 1 < lines.length && isTableSeparator(lines[i + 1] ?? '')) {
      const tableLines = [line];
      i += 1;
      while (i < lines.length && /^\s*\|.+\|\s*$/.test(lines[i] ?? '')) {
        tableLines.push(lines[i] ?? '');
        i += 1;
      }
      html.push(renderTable(tableLines));
      continue;
    }

    const heading = /^(#{1,6})\s+(.+)$/.exec(line);
    if (heading) {
      const level = heading[1].length;
      html.push(`<h${level}>${renderInline(heading[2])}</h${level}>`);
      i += 1;
      continue;
    }

    if (/^\s*---+\s*$/.test(line) || /^\s*\*\*\*+\s*$/.test(line)) {
      html.push('<hr />');
      i += 1;
      continue;
    }

    if (/^\s*>\s?/.test(line)) {
      const quote: string[] = [];
      while (i < lines.length && /^\s*>\s?/.test(lines[i] ?? '')) {
        quote.push((lines[i] ?? '').replace(/^\s*>\s?/, ''));
        i += 1;
      }
      html.push(`<blockquote>${renderInline(quote.join(' '))}</blockquote>`);
      continue;
    }

    if (/^\s*[-*]\s+/.test(line)) {
      const items: string[] = [];
      while (i < lines.length && /^\s*[-*]\s+/.test(lines[i] ?? '')) {
        items.push(`<li>${renderInline((lines[i] ?? '').replace(/^\s*[-*]\s+/, ''))}</li>`);
        i += 1;
      }
      html.push(`<ul>${items.join('')}</ul>`);
      continue;
    }

    if (/^\s*\d+\.\s+/.test(line)) {
      const items: string[] = [];
      while (i < lines.length && /^\s*\d+\.\s+/.test(lines[i] ?? '')) {
        items.push(`<li>${renderInline((lines[i] ?? '').replace(/^\s*\d+\.\s+/, ''))}</li>`);
        i += 1;
      }
      html.push(`<ol>${items.join('')}</ol>`);
      continue;
    }

    if (!line.trim()) {
      i += 1;
      continue;
    }

    const paragraph: string[] = [line];
    i += 1;
    while (
      i < lines.length &&
      (lines[i] ?? '').trim() &&
      !/^(#{1,6})\s+/.test(lines[i] ?? '') &&
      !/^\s*[-*]\s+/.test(lines[i] ?? '') &&
      !/^\s*\d+\.\s+/.test(lines[i] ?? '') &&
      !(lines[i] ?? '').startsWith('```') &&
      !/^\s*>\s?/.test(lines[i] ?? '')
    ) {
      paragraph.push(lines[i] ?? '');
      i += 1;
    }
    html.push(`<p>${renderInline(paragraph.join(' '))}</p>`);
  }

  return html.join('');
}

function renderInline(text: string): string {
  let value = escapeHtml(text);
  value = value.replace(/`([^`]+)`/g, '<code>$1</code>');
  value = value.replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>');
  value = value.replace(/__(.+?)__/g, '<strong>$1</strong>');
  value = value.replace(/(^|[^*])\*(?!\*)([^*]+)\*(?!\*)/g, '$1<em>$2</em>');
  value = value.replace(
    /\[([^\]]+)\]\((https?:[^)\s]+)\)/g,
    '<a href="$2" rel="noopener noreferrer" target="_blank">$1</a>'
  );
  return value;
}

function renderTable(lines: string[]): string {
  const rows = lines
    .filter((line) => !isTableSeparator(line))
    .map((line) => splitTableRow(line));
  if (rows.length === 0) {
    return '';
  }

  const header = rows[0];
  const body = rows.slice(1);
  const head = `<thead><tr>${header.map((cell) => `<th>${renderInline(cell)}</th>`).join('')}</tr></thead>`;
  const bodyHtml =
    body.length === 0
      ? ''
      : `<tbody>${body.map((row) => `<tr>${row.map((cell) => `<td>${renderInline(cell)}</td>`).join('')}</tr>`).join('')}</tbody>`;
  return `<table>${head}${bodyHtml}</table>`;
}

function isTableSeparator(line: string): boolean {
  return /^\s*\|?\s*:?-{3,}:?\s*(\|\s*:?-{3,}:?\s*)+\|?\s*$/.test(line);
}

function splitTableRow(line: string): string[] {
  return line
    .trim()
    .replace(/^\|/, '')
    .replace(/\|$/, '')
    .split('|')
    .map((cell) => cell.trim());
}

function escapeHtml(value: string): string {
  return value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}
