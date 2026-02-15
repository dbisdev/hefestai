/**
 * Rule Query Component
 * Provides RAG-powered semantic search across game system manuals.
 * Allows users to ask natural language questions about rules and lore.
 * 
 * Features:
 * - Full keyboard navigation (Tab, Escape)
 * - ARIA labels and roles for screen readers
 * - Focus management for modal dialog
 * - Live region announcements for search results
 * - GameSystem selector (always visible, pre-selects campaign's system)
 * - Only shows game systems that have RAG documents
 */

import React, { useState, useRef, useEffect, useCallback, KeyboardEvent } from 'react';
import { documentService, gameSystemService } from '../core/services/api';
import type { DocumentSearchResult, SemanticSearchResult } from '../core/services/api';
import type { GameSystem } from '../core/types';

/**
 * Game system with document availability info
 */
interface GameSystemWithDocs extends GameSystem {
  hasDocuments: boolean;
}

interface RuleQueryProps {
  /** Callback to close the modal */
  onClose: () => void;
  /** Optional game system ID to pre-select (from campaign) */
  gameSystemId?: string;
  /** Optional game system name for display (unused, kept for compatibility) */
  gameSystemName?: string;
  /** Optional campaign owner (Master) ID - used for Players to see Master's documents */
  campaignOwnerId?: string;
}

/**
 * RuleQuery Modal Component
 * 
 * Displays a search interface for querying game rules using RAG.
 * Results include relevant document excerpts and optionally an AI-generated answer.
 * Shows a GameSystem selector with only systems that have documents loaded.
 */
const RuleQuery: React.FC<RuleQueryProps> = ({ onClose, gameSystemId: propGameSystemId, campaignOwnerId }) => {
  // Normalize empty string to undefined for consistent falsy checks
  const initialGameSystemId = propGameSystemId || undefined;
  
  // State
  const [query, setQuery] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [results, setResults] = useState<SemanticSearchResult | null>(null);
  const [generateAnswer, setGenerateAnswer] = useState(true);
  const [showSettings, setShowSettings] = useState(false);
  const [threshold, setThreshold] = useState(0.6);
  const [resultLimit, setResultLimit] = useState(6);
  
  // GameSystem selector state - always load all systems with their document counts
  const [gameSystemsWithDocs, setGameSystemsWithDocs] = useState<GameSystemWithDocs[]>([]);
  const [selectedGameSystemId, setSelectedGameSystemId] = useState<string>('');
  const [isLoadingGameSystems, setIsLoadingGameSystems] = useState(true);

  // Refs for focus management
  const inputRef = useRef<HTMLInputElement>(null);
  const modalRef = useRef<HTMLDivElement>(null);

  // Accessibility announcement
  const [announcement, setAnnouncement] = useState('');

  // Computed: get current selected system's document availability
  const selectedSystem = gameSystemsWithDocs.find(gs => gs.id === selectedGameSystemId);
  const hasDocuments = selectedSystem?.hasDocuments ?? false;

  /**
   * Announce to screen readers
   */
  const announce = useCallback((message: string) => {
    setAnnouncement(message);
    setTimeout(() => setAnnouncement(''), 1000);
  }, []);

  /**
   * Handle escape key to close modal
   */
  const handleKeyDown = useCallback((e: KeyboardEvent) => {
    if (e.key === 'Escape') {
      onClose();
    }
  }, [onClose]);

  /**
   * Focus input on mount
   */
  useEffect(() => {
    inputRef.current?.focus();
  }, []);

  /**
   * Load all game systems and check document availability on mount.
   * Uses the Player-accessible /available endpoint that returns Admin-shared docs for Players,
   * and Admin + own docs for Masters.
   * When campaignOwnerId is provided, also includes documents from the campaign's Master.
   * Pre-selects the campaign's game system if available, otherwise first with documents.
   */
  useEffect(() => {
    const loadGameSystemsWithDocs = async () => {
      setIsLoadingGameSystems(true);
      try {
        // Load all game systems
        const systems = await gameSystemService.getAll();
        
        // Check document availability for each system in parallel
        // Pass campaignOwnerId so Players can see their Master's documents
        const systemsWithAvailability = await Promise.all(
          systems.map(async (system): Promise<GameSystemWithDocs> => {
            try {
              const availability = await documentService.checkDocumentAvailability(system.id, campaignOwnerId);
              return { ...system, hasDocuments: availability.hasDocuments };
            } catch {
              return { ...system, hasDocuments: false };
            }
          })
        );
        
        setGameSystemsWithDocs(systemsWithAvailability);
        
        // Pre-select: campaign's system if provided, otherwise first with documents
        if (systemsWithAvailability.length > 0) {
          if (initialGameSystemId && systemsWithAvailability.some(s => s.id === initialGameSystemId)) {
            setSelectedGameSystemId(initialGameSystemId);
          } else {
            // Select first system with documents, or first system if none have docs
            const firstWithDocs = systemsWithAvailability.find(s => s.hasDocuments);
            setSelectedGameSystemId(firstWithDocs?.id || systemsWithAvailability[0].id);
          }
        }
      } catch (err) {
        console.error('Failed to load game systems:', err);
      } finally {
        setIsLoadingGameSystems(false);
      }
    };
    
    loadGameSystemsWithDocs();
  }, [initialGameSystemId, campaignOwnerId]);

  /**
   * Perform semantic search
   */
  const handleSearch = async () => {
    if (!query.trim()) {
      setError('Por favor, introduce una consulta');
      return;
    }

    if (!selectedGameSystemId) {
      setError('Por favor, selecciona un sistema de juego');
      return;
    }

    if (!hasDocuments) {
      setError('El sistema de juego seleccionado no tiene manuales cargados.');
      return;
    }

    setIsLoading(true);
    setError(null);
    setResults(null);

    try {
      const searchResult = await documentService.semanticSearch({
        query: query.trim(),
        gameSystemId: selectedGameSystemId,
        generateAnswer,
        limit: resultLimit,
        threshold,
        masterId: campaignOwnerId, // Include campaign Master's documents for Players
      });

      setResults(searchResult);
      
      const resultCount = searchResult.documents.length;
      announce(
        resultCount > 0
          ? `Found ${resultCount} result${resultCount !== 1 ? 's' : ''}${searchResult.generatedAnswer ? ' with AI answer' : ''}`
          : 'No results found'
      );
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Search failed';
      setError(message);
      announce('Search failed');
    } finally {
      setIsLoading(false);
    }
  };

  /**
   * Handle form submission
   */
  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    handleSearch();
  };

  /**
   * Format similarity score as percentage
   */
  const formatScore = (score: number): string => {
    return `${Math.round(score * 100)}%`;
  };

  /**
   * Truncate text to specified length
   */
  const truncateText = (text: string, maxLength: number = 300): string => {
    if (text.length <= maxLength) return text;
    return text.substring(0, maxLength).trim() + '...';
  };

  return (
    <div
      className="fixed inset-0 bg-black/90 flex items-center justify-center z-50"
      onKeyDown={handleKeyDown}
      role="dialog"
      aria-modal="true"
      aria-labelledby="rule-query-title"
    >
      {/* Screen reader announcement */}
      <div
        role="status"
        aria-live="polite"
        aria-atomic="true"
        className="sr-only"
      >
        {announcement}
      </div>

      <div
        ref={modalRef}
        className="bg-background-dark border-2 border-primary w-full max-w-4xl max-h-[90vh] overflow-hidden flex flex-col mx-4"
      >
        {/* Header */}
        <div className="border-b border-primary/30 p-4 flex justify-between items-start">
          <div className="flex-1">
            <h2
              id="rule-query-title"
              className="text-xl font-display uppercase tracking-wider text-primary text-glow"
            >
              CONSULTA DE REGLAS
            </h2>
            {/* Game System Selector - always visible */}
            <div className="flex items-center gap-2 mt-2 flex-wrap">
              <label className="text-xs text-primary/60 uppercase tracking-wider">
                Sistema:
              </label>
              {isLoadingGameSystems ? (
                <span className="text-xs text-primary/40 italic">Cargando sistemas...</span>
              ) : gameSystemsWithDocs.length === 0 ? (
                <span className="text-xs text-red-400 flex items-center gap-1">
                  <span className="material-icons text-xs">warning</span>
                  No hay sistemas disponibles
                </span>
              ) : (
                <>
                  <select
                    value={selectedGameSystemId}
                    onChange={(e) => setSelectedGameSystemId(e.target.value)}
                    className="bg-black/50 border border-primary/40 px-2 py-1 text-xs text-primary focus:border-primary focus:outline-none font-mono"
                    aria-label="Seleccionar sistema de juego"
                  >
                    {gameSystemsWithDocs.map((gs) => (
                      <option 
                        key={gs.id} 
                        value={gs.id}                        
                      >
                        {gs.name}
                      </option>
                    ))}
                  </select>
                  {/* Document availability indicator */}
                  {hasDocuments ? (
                    <span className="text-xs text-green-400 flex items-center gap-1">
                      <span className="material-icons text-xs">check_circle</span>
                      Manuales disponibles
                    </span>
                  ) : (
                    <span className="text-xs text-red-400 flex items-center gap-1">
                      <span className="material-icons text-xs">warning</span>
                      Sin manuales cargados
                    </span>
                  )}
                </>
              )}
            </div>
          </div>
          <button
            onClick={onClose}
            className="text-primary hover:text-primary/80 transition-colors p-2"
            aria-label="Cerrar consulta de reglas"
          >
            <span className="material-icons">close</span>
          </button>
        </div>

        {/* Search Form */}
        <form onSubmit={handleSubmit} className="p-4 border-b border-primary/20">
          <div className="flex gap-2">
            <input
              ref={inputRef}
              type="text"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder="¿Qué quieres saber sobre las reglas?"
              className="md:flex-1 flex-none bg-black/50 border border-primary/40 px-2 md:px-3 py-2 text-primary placeholder-primary/40 focus:border-primary focus:outline-none font-mono"
              aria-label="Consulta de reglas"
              disabled={isLoading}
            />
            <button
              type="submit"
              disabled={isLoading || !query.trim() || !hasDocuments || isLoadingGameSystems}
              className="border border-primary px-3 py-2 text-xs uppercase hover:bg-primary hover:text-black transition-colors text-primary font-bold disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
              aria-label="Buscar"
            >
              {isLoading ? (
                <span className="material-icons animate-spin">sync</span>
              ) : (
                <span className="material-icons">search</span>
              )}
              <span className="hidden sm:inline">BUSCAR</span>
            </button>
            <button
              type="button"
              onClick={() => setShowSettings(!showSettings)}
              className={`border px-3 py-2 transition-colors ${
                showSettings
                  ? 'bg-primary text-black border-primary'
                  : 'border-primary/40 text-primary hover:border-primary'
              }`}
              aria-label="Configuración de búsqueda"
              aria-expanded={showSettings}
            >
              <span className="material-icons">tune</span>
            </button>
          </div>

          {/* Settings Panel */}
          {showSettings && (
            <div className="mt-4 p-4 bg-black/30 border border-primary/20 grid grid-cols-1 md:grid-cols-3 gap-4">
              <div>
                <label className="block text-xs text-primary/60 uppercase mb-2">
                  Generar Respuesta IA
                </label>
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={generateAnswer}
                    onChange={(e) => setGenerateAnswer(e.target.checked)}
                    className="w-4 h-4 accent-primary"
                  />
                  <span className="text-primary text-sm">
                    {generateAnswer ? 'Activado' : 'Desactivado'}
                  </span>
                </label>
              </div>
              <div>
                <label className="block text-xs text-primary/60 uppercase mb-2">
                  Umbral de Similitud: {Math.round(threshold * 100)}%
                </label>
                <input
                  type="range"
                  min="0.5"
                  max="0.95"
                  step="0.05"
                  value={threshold}
                  onChange={(e) => setThreshold(parseFloat(e.target.value))}
                  className="w-full accent-primary"
                />
              </div>
              <div>
                <label className="block text-xs text-primary/60 uppercase mb-2">
                  Máximo de Resultados: {resultLimit}
                </label>
                <input
                  type="range"
                  min="1"
                  max="10"
                  step="1"
                  value={resultLimit}
                  onChange={(e) => setResultLimit(parseInt(e.target.value))}
                  className="w-full accent-primary"
                />
              </div>
            </div>
          )}
        </form>

        {/* Results Area */}
        <div className="flex-1 overflow-y-auto p-4">
          {/* Error State */}
          {error && (
            <div className="bg-red-900/30 border border-red-500/50 p-4 mb-4 flex items-start gap-3">
              <span className="material-icons text-red-400">error</span>
              <div>
                <p className="text-red-400 font-bold text-sm uppercase">Error</p>
                <p className="text-red-300 text-sm">{error}</p>
              </div>
            </div>
          )}

          {/* Loading State */}
          {isLoading && (
            <div className="flex flex-col items-center justify-center py-12">
              <div className="w-12 h-12 border-2 border-primary border-t-transparent rounded-full animate-spin mb-4" />
              <p className="text-primary/60 text-sm uppercase tracking-wider">
                Buscando en manuales...
              </p>
            </div>
          )}

          {/* Results */}
          {results && !isLoading && (
            <div className="space-y-6">
              {/* AI Generated Answer */}
              {results.generatedAnswer && (
                <div className="bg-primary/10 border border-primary/40 p-4">
                  <div className="flex items-center gap-2 mb-3">
                    <span className="material-icons text-primary">auto_awesome</span>
                    <h3 className="text-primary font-bold text-sm uppercase tracking-wider">
                      Respuesta Generada por IA
                    </h3>
                  </div>
                  <p className="text-primary/90 leading-relaxed whitespace-pre-wrap">
                    {results.generatedAnswer}
                  </p>
                </div>
              )}

              {/* Source Documents */}
              <div>
                <h3 className="text-primary/60 text-xs uppercase tracking-wider mb-3 flex items-center gap-2">
                  <span className="material-icons text-sm">source</span>
                  Fuentes Relevantes ({results.documents.length})
                </h3>
                
                {results.documents.length === 0 ? (
                  <div className="text-center py-8">
                    <span className="material-icons text-4xl text-primary/30 mb-2">search_off</span>
                    <p className="text-primary/50 text-sm">
                      No se encontraron resultados. Intenta reformular tu consulta.
                    </p>
                  </div>
                ) : (
                  <div className="space-y-3">
                    {results.documents.map((result, index) => (
                      <DocumentResultCard key={result.document.id || index} result={result} />
                    ))}
                  </div>
                )}
              </div>
            </div>
          )}

          {/* Empty State */}
          {!results && !isLoading && !error && (
            <div className="flex flex-col items-center justify-center py-12 text-center">
              <span className="material-icons text-6xl text-primary/20 mb-4">menu_book</span>
              <h3 className="text-primary/60 text-lg uppercase tracking-wider mb-2">
                Consulta los Manuales
              </h3>
              <p className="text-primary/40 text-sm max-w-md">
                Escribe una pregunta sobre las reglas del juego y buscaré en los manuales
                cargados para encontrar la información relevante.
              </p>
              <div className="mt-6 grid grid-cols-1 sm:grid-cols-2 gap-2 text-xs">
                <ExampleQuery 
                  query="¿Cómo funciona el combate?" 
                  onClick={() => setQuery('¿Cómo funciona el combate?')} 
                />
                <ExampleQuery 
                  query="¿Qué talentos tiene un piloto y su descripción?" 
                  onClick={() => setQuery('¿Qué talentos tiene un piloto y su descripción?')} 
                />
                <ExampleQuery 
                  query="¿Cuánto daño hace un rifle M41A?" 
                  onClick={() => setQuery('¿Cuánto daño hace un rifle M41A?')} 
                />
                <ExampleQuery 
                  query="¿Cómo se crea un personaje paso a paso?" 
                  onClick={() => setQuery('¿Cómo se crea un personaje paso a paso?')} 
                />
              </div>
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="border-t border-primary/30 p-3 flex justify-between items-center text-xs text-primary/40">
          <span>RAG Powered Search • Vector Similarity</span>
          <span>ESC para cerrar</span>
        </div>
      </div>
    </div>
  );
};

/**
 * Example query button component
 */
const ExampleQuery: React.FC<{ query: string; onClick: () => void }> = ({ query, onClick }) => (
  <button
    onClick={onClick}
    className="text-left border border-primary/20 px-3 py-2 text-primary/60 hover:border-primary/40 hover:text-primary transition-colors"
  >
    <span className="material-icons text-xs mr-1 align-middle">arrow_forward</span>
    {query}
  </button>
);

/**
 * Document result card component
 */
const DocumentResultCard: React.FC<{ result: DocumentSearchResult }> = ({ result }) => {
  const [isExpanded, setIsExpanded] = useState(false);
  const { document, similarityScore } = result;
  
  const scoreColor = similarityScore >= 0.8 ? 'text-green-400' : 
                     similarityScore >= 0.7 ? 'text-yellow-400' : 
                     'text-orange-400';

  return (
    <div className="bg-black/30 border border-primary/20 hover:border-primary/40 transition-colors">
      <div className="p-3 flex justify-between items-start gap-4">
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 mb-1">
            <span className="material-icons text-primary/60 text-sm">description</span>
            <h4 className="text-primary font-bold text-sm truncate">
              {document.title}
            </h4>
          </div>
          {document.source && (
            <p className="text-primary/40 text-xs mb-2 truncate">
              Fuente: {document.source}
            </p>
          )}
        </div>
        <div className="flex items-center gap-2">
          <span className={`text-xs font-mono ${scoreColor}`}>
            {Math.round(similarityScore * 100)}%
          </span>
          <button
            onClick={() => setIsExpanded(!isExpanded)}
            className="text-primary/60 hover:text-primary transition-colors"
            aria-expanded={isExpanded}
            aria-label={isExpanded ? 'Contraer contenido' : 'Expandir contenido'}
          >
            <span className="material-icons text-sm">
              {isExpanded ? 'expand_less' : 'expand_more'}
            </span>
          </button>
        </div>
      </div>
      
      <div className={`px-3 pb-3 ${isExpanded ? '' : 'max-h-20 overflow-hidden'}`}>
        <p className="text-primary/70 text-sm leading-relaxed whitespace-pre-wrap">
          {isExpanded ? document.content : truncateText(document.content, 200)}
        </p>
      </div>
      
      {!isExpanded && document.content.length > 200 && (
        <div className="h-6 bg-gradient-to-t from-black/80 to-transparent -mt-6 relative pointer-events-none" />
      )}
    </div>
  );
};

/**
 * Truncate text helper
 */
const truncateText = (text: string, maxLength: number): string => {
  if (text.length <= maxLength) return text;
  return text.substring(0, maxLength).trim() + '...';
};

export default RuleQuery;
