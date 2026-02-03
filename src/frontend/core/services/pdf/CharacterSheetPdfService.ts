/**
 * Character Sheet PDF Service
 * Handles export and import of character sheets as PDF documents
 * 
 * Single Responsibility: PDF generation and parsing for character data
 */

import { jsPDF } from 'jspdf';
import type { LoreEntity, CharacterData, NpcData, EnemyData } from '@core/types';

/**
 * PDF export configuration
 */
interface PdfExportOptions {
  /** Include character image in PDF */
  includeImage?: boolean;
  /** PDF page format */
  format?: 'a4' | 'letter';
  /** PDF orientation */
  orientation?: 'portrait' | 'landscape';
}

/**
 * Exported character sheet data structure
 * Used for JSON embedding in PDF metadata
 */
interface CharacterSheetData {
  version: string;
  exportedAt: string;
  entity: {
    name: string;
    description?: string;
    entityType: string;
    attributes: Record<string, unknown>;
    metadata?: Record<string, unknown>;
  };
}

/**
 * Color palette for PDF (RGB values 0-255)
 */
const COLORS = {
  primary: [37, 244, 106] as [number, number, number],      // #25f46a - cyberpunk green
  background: [18, 18, 24] as [number, number, number],     // #121218 - dark background
  surface: [30, 30, 40] as [number, number, number],        // #1e1e28 - surface
  text: [255, 255, 255] as [number, number, number],        // white
  textMuted: [150, 150, 160] as [number, number, number],   // muted text
  accent: [100, 200, 255] as [number, number, number],      // cyan accent
};

/**
 * CharacterSheetPdfService
 * Generates styled PDF character sheets with cyberpunk aesthetics
 */
export class CharacterSheetPdfService {
  private static readonly VERSION = '1.0.0';
  private static readonly FONT_SIZE = {
    title: 24,
    subtitle: 14,
    heading: 12,
    body: 10,
    small: 8,
  };

  /**
   * Export a character entity to PDF
   * @param entity - The character entity to export
   * @param options - Export options
   * @returns Promise resolving to PDF blob
   */
  static async exportToPdf(
    entity: LoreEntity,
    options: PdfExportOptions = {}
  ): Promise<Blob> {
    const {
      includeImage = true,
      format = 'a4',
      orientation = 'portrait',
    } = options;

    const doc = new jsPDF({
      orientation,
      unit: 'mm',
      format,
    });

    const pageWidth = doc.internal.pageSize.getWidth();
    const pageHeight = doc.internal.pageSize.getHeight();
    const margin = 15;
    let yPos = margin;

    // Draw background
    doc.setFillColor(...COLORS.background);
    doc.rect(0, 0, pageWidth, pageHeight, 'F');

    // Draw header border
    doc.setDrawColor(...COLORS.primary);
    doc.setLineWidth(0.5);
    doc.line(margin, margin + 5, pageWidth - margin, margin + 5);

    // Title
    yPos = this.drawTitle(doc, entity.name, margin, yPos, pageWidth);

    // Entity type badge
    yPos = this.drawBadge(doc, entity.entityType.toUpperCase().replace('_', ' '), margin, yPos);

    // Horizontal divider
    yPos += 5;
    doc.setDrawColor(...COLORS.primary);
    doc.setLineWidth(0.3);
    doc.line(margin, yPos, pageWidth - margin, yPos);
    yPos += 10;

    // Two-column layout
    const colWidth = (pageWidth - margin * 3) / 2;
    const leftCol = margin;
    const rightCol = margin + colWidth + margin;

    // Left column: Image and basic info
    let leftY = yPos;
    
    // Character image
    if (includeImage && entity.imageUrl) {
      leftY = await this.drawImage(doc, entity.imageUrl, leftCol, leftY, colWidth);
    }

    // Description
    leftY = this.drawSection(doc, 'BIOGRAFÍA', entity.description || 'Sin descripción disponible.', leftCol, leftY, colWidth);

    // Right column: Stats and attributes
    let rightY = yPos;

    // Stats grid
    rightY = this.drawStatsGrid(doc, entity.attributes || {}, rightCol, rightY, colWidth);

    // Metadata
    if (entity.metadata && Object.keys(entity.metadata).length > 0) {
      rightY = this.drawMetadata(doc, entity.metadata, rightCol, rightY, colWidth);
    }

    // Footer
    this.drawFooter(doc, entity, pageWidth, pageHeight, margin);

    // Embed JSON data in PDF metadata for import
    const sheetData: CharacterSheetData = {
      version: this.VERSION,
      exportedAt: new Date().toISOString(),
      entity: {
        name: entity.name,
        description: entity.description,
        entityType: entity.entityType,
        attributes: entity.attributes || {},
        metadata: entity.metadata,
      },
    };
    
    doc.setProperties({
      title: `Character Sheet - ${entity.name}`,
      subject: 'Character Sheet Export',
      author: 'Hefestai Character Generator',
      keywords: 'character, rpg, hefestai',
      creator: 'Hefestai PDF Service',
    });

    // Store JSON in custom metadata field (accessible via PDF properties)
    // We'll also embed it as a hidden text element for reliable extraction
    doc.setFontSize(1);
    doc.setTextColor(COLORS.background[0], COLORS.background[1], COLORS.background[2]);
    doc.text(`HEFESTAI_DATA:${JSON.stringify(sheetData)}:END_HEFESTAI_DATA`, 1, 1);

    return doc.output('blob');
  }

  /**
   * Draw the character title
   */
  private static drawTitle(
    doc: jsPDF,
    title: string,
    x: number,
    y: number,
    pageWidth: number
  ): number {
    doc.setFontSize(this.FONT_SIZE.title);
    doc.setTextColor(...COLORS.primary);
    doc.setFont('helvetica', 'bold');
    
    const titleY = y + 15;
    doc.text(title.toUpperCase(), x, titleY);
    
    // Decorative elements
    doc.setFillColor(...COLORS.primary);
    doc.rect(x, titleY + 2, 30, 1, 'F');
    
    return titleY + 8;
  }

  /**
   * Draw an entity type badge
   */
  private static drawBadge(
    doc: jsPDF,
    text: string,
    x: number,
    y: number
  ): number {
    doc.setFontSize(this.FONT_SIZE.small);
    doc.setTextColor(...COLORS.background);
    doc.setFillColor(...COLORS.primary);
    
    const badgeWidth = doc.getTextWidth(text) + 6;
    const badgeHeight = 5;
    
    doc.rect(x, y, badgeWidth, badgeHeight, 'F');
    doc.text(text, x + 3, y + 3.5);
    
    return y + badgeHeight + 5;
  }

  /**
   * Draw character image from URL
   */
  private static async drawImage(
    doc: jsPDF,
    imageUrl: string,
    x: number,
    y: number,
    maxWidth: number
  ): Promise<number> {
    try {
      // Handle data URLs and remote URLs
      let imageData = imageUrl;
      
      if (!imageUrl.startsWith('data:')) {
        // For remote URLs, we need to fetch and convert to base64
        const response = await fetch(imageUrl);
        const blob = await response.blob();
        imageData = await this.blobToBase64(blob);
      }

      const imgHeight = maxWidth * 0.75; // 4:3 aspect ratio
      
      // Draw border
      doc.setDrawColor(...COLORS.primary);
      doc.setLineWidth(0.5);
      doc.rect(x, y, maxWidth, imgHeight);
      
      // Add image
      doc.addImage(imageData, 'JPEG', x + 1, y + 1, maxWidth - 2, imgHeight - 2);
      
      return y + imgHeight + 8;
    } catch (error) {
      console.warn('Failed to load image for PDF:', error);
      // Draw placeholder
      doc.setFillColor(...COLORS.surface);
      doc.rect(x, y, maxWidth, maxWidth * 0.75, 'F');
      doc.setFontSize(this.FONT_SIZE.small);
      doc.setTextColor(...COLORS.textMuted);
      doc.text('IMAGE NOT AVAILABLE', x + maxWidth / 2, y + maxWidth * 0.375, { align: 'center' });
      
      return y + maxWidth * 0.75 + 8;
    }
  }

  /**
   * Draw a text section with heading
   */
  private static drawSection(
    doc: jsPDF,
    heading: string,
    content: string,
    x: number,
    y: number,
    width: number
  ): number {
    // Heading
    doc.setFontSize(this.FONT_SIZE.heading);
    doc.setTextColor(...COLORS.primary);
    doc.setFont('helvetica', 'bold');
    doc.text(heading, x, y);
    
    // Underline
    doc.setDrawColor(...COLORS.primary);
    doc.setLineWidth(0.2);
    doc.line(x, y + 1, x + doc.getTextWidth(heading), y + 1);
    
    y += 6;
    
    // Content
    doc.setFontSize(this.FONT_SIZE.body);
    doc.setTextColor(...COLORS.text);
    doc.setFont('helvetica', 'normal');
    
    const lines = doc.splitTextToSize(content, width);
    doc.text(lines, x, y);
    
    return y + lines.length * 4 + 8;
  }

  /**
   * Draw stats grid
   */
  private static drawStatsGrid(
    doc: jsPDF,
    attributes: Record<string, unknown>,
    x: number,
    y: number,
    width: number
  ): number {
    // Section heading
    doc.setFontSize(this.FONT_SIZE.heading);
    doc.setTextColor(...COLORS.primary);
    doc.setFont('helvetica', 'bold');
    doc.text('ESTADÍSTICAS', x, y);
    
    doc.setDrawColor(...COLORS.primary);
    doc.setLineWidth(0.2);
    doc.line(x, y + 1, x + doc.getTextWidth('ESTADÍSTICAS'), y + 1);
    
    y += 8;
    
    // Filter numeric stats
    const stats = Object.entries(attributes)
      .filter(([_, value]) => typeof value === 'number')
      .slice(0, 6); // Max 6 stats
    
    if (stats.length === 0) {
      doc.setFontSize(this.FONT_SIZE.body);
      doc.setTextColor(...COLORS.textMuted);
      doc.text('Sin estadísticas disponibles', x, y);
      return y + 10;
    }

    const cols = 3;
    const cellWidth = width / cols;
    const cellHeight = 20;
    
    stats.forEach(([key, value], index) => {
      const col = index % cols;
      const row = Math.floor(index / cols);
      const cellX = x + col * cellWidth;
      const cellY = y + row * cellHeight;
      
      // Cell background
      doc.setFillColor(...COLORS.surface);
      doc.rect(cellX, cellY, cellWidth - 2, cellHeight - 2, 'F');
      
      // Cell border
      doc.setDrawColor(...COLORS.primary);
      doc.setLineWidth(0.3);
      doc.rect(cellX, cellY, cellWidth - 2, cellHeight - 2);
      
      // Stat label
      doc.setFontSize(this.FONT_SIZE.small);
      doc.setTextColor(...COLORS.textMuted);
      doc.text(key.toUpperCase(), cellX + (cellWidth - 2) / 2, cellY + 5, { align: 'center' });
      
      // Stat value
      doc.setFontSize(this.FONT_SIZE.subtitle);
      doc.setTextColor(...COLORS.primary);
      doc.setFont('helvetica', 'bold');
      doc.text(String(value), cellX + (cellWidth - 2) / 2, cellY + 13, { align: 'center' });
      doc.setFont('helvetica', 'normal');
    });
    
    const rows = Math.ceil(stats.length / cols);
    return y + rows * cellHeight + 8;
  }

  /**
   * Draw metadata section
   */
  private static drawMetadata(
    doc: jsPDF,
    metadata: Record<string, unknown>,
    x: number,
    y: number,
    width: number
  ): number {
    doc.setFontSize(this.FONT_SIZE.heading);
    doc.setTextColor(...COLORS.primary);
    doc.setFont('helvetica', 'bold');
    doc.text('METADATOS', x, y);
    
    doc.setDrawColor(...COLORS.primary);
    doc.setLineWidth(0.2);
    doc.line(x, y + 1, x + doc.getTextWidth('METADATOS'), y + 1);
    
    y += 6;
    
    doc.setFontSize(this.FONT_SIZE.small);
    doc.setFont('helvetica', 'normal');
    
    Object.entries(metadata).slice(0, 5).forEach(([key, value]) => {
      doc.setTextColor(...COLORS.textMuted);
      doc.text(`${key}:`, x, y);
      doc.setTextColor(...COLORS.text);
      const valueStr = typeof value === 'object' ? JSON.stringify(value) : String(value);
      const truncated = valueStr.length > 30 ? valueStr.substring(0, 27) + '...' : valueStr;
      doc.text(truncated, x + 25, y);
      y += 4;
    });
    
    return y + 5;
  }

  /**
   * Draw footer with export info
   */
  private static drawFooter(
    doc: jsPDF,
    entity: LoreEntity,
    pageWidth: number,
    pageHeight: number,
    margin: number
  ): void {
    const footerY = pageHeight - margin;
    
    // Footer line
    doc.setDrawColor(...COLORS.primary);
    doc.setLineWidth(0.3);
    doc.line(margin, footerY - 5, pageWidth - margin, footerY - 5);
    
    // Footer text
    doc.setFontSize(this.FONT_SIZE.small);
    doc.setTextColor(...COLORS.textMuted);
    
    const leftText = `ID: ${entity.id.substring(0, 8)}`;
    const rightText = `Exportado: ${new Date().toLocaleDateString()}`;
    const centerText = 'HEFESTAI // CHARACTER SHEET';
    
    doc.text(leftText, margin, footerY);
    doc.text(centerText, pageWidth / 2, footerY, { align: 'center' });
    doc.text(rightText, pageWidth - margin, footerY, { align: 'right' });
  }

  /**
   * Convert blob to base64
   */
  private static blobToBase64(blob: Blob): Promise<string> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onloadend = () => resolve(reader.result as string);
      reader.onerror = reject;
      reader.readAsDataURL(blob);
    });
  }

  /**
   * Import character data from PDF
   * Extracts embedded JSON data from PDF file
   * @param file - PDF file to import
   * @returns Promise resolving to extracted character data or null
   */
  static async importFromPdf(file: File): Promise<CharacterSheetData | null> {
    try {
      const text = await this.extractTextFromPdf(file);
      
      // Look for embedded data marker
      const startMarker = 'HEFESTAI_DATA:';
      const endMarker = ':END_HEFESTAI_DATA';
      
      const startIndex = text.indexOf(startMarker);
      const endIndex = text.indexOf(endMarker);
      
      if (startIndex === -1 || endIndex === -1) {
        console.warn('No embedded Hefestai data found in PDF');
        return null;
      }
      
      const jsonStr = text.substring(startIndex + startMarker.length, endIndex);
      const data = JSON.parse(jsonStr) as CharacterSheetData;
      
      // Validate version compatibility
      if (!data.version || !data.entity) {
        throw new Error('Invalid character sheet data format');
      }
      
      return data;
    } catch (error) {
      console.error('Failed to import PDF:', error);
      return null;
    }
  }

  /**
   * Extract text content from PDF file
   * Uses basic PDF text extraction
   */
  private static async extractTextFromPdf(file: File): Promise<string> {
    const arrayBuffer = await file.arrayBuffer();
    const bytes = new Uint8Array(arrayBuffer);
    
    // Convert to string and look for text streams
    // This is a simplified extraction - for production, use pdf.js or similar
    let text = '';
    const decoder = new TextDecoder('utf-8', { fatal: false });
    const content = decoder.decode(bytes);
    
    // Look for our embedded marker directly in the PDF content
    // The marker is embedded as literal text in the PDF
    const markerMatch = content.match(/HEFESTAI_DATA:(.+?):END_HEFESTAI_DATA/);
    if (markerMatch) {
      text = markerMatch[0];
    }
    
    return text;
  }

  /**
   * Download PDF blob as file
   * @param blob - PDF blob to download
   * @param filename - Desired filename
   */
  static downloadPdf(blob: Blob, filename: string): void {
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename.endsWith('.pdf') ? filename : `${filename}.pdf`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  }
}

export default CharacterSheetPdfService;
