/**
 * Home Page Component
 * Landing page with information about HefestAI and access to login/signup
 * Implements retro-futuristic sci-fi terminal aesthetic
 */

import React from 'react';

/**
 * Props for the Home component
 */
interface HomeProps {
  /** Callback to navigate to login page */
  onLogin: () => void;
  /** Callback to navigate to signup page */
  onSignup: () => void;
}

/**
 * Home page component - Initial landing page for the application
 * Features:
 * - Hero section with call-to-action buttons
 * - 3-column feature highlights
 * - Retro terminal aesthetic with CRT effects
 * - System status footer
 */
const Home: React.FC<HomeProps> = ({ onLogin, onSignup }) => {
  return (
    <div className="h-full bg-background-dark font-mono text-primary flex flex-col relative overflow-y-auto custom-scrollbar scroll-smooth">
      {/* Background Grid - Fixed so it doesn't scroll with content but stays behind */}
      <div className="fixed inset-0 opacity-5 pointer-events-none" 
           style={{ backgroundImage: 'linear-gradient(#25f46a 1px, transparent 1px), linear-gradient(90deg, #25f46a 1px, transparent 1px)', backgroundSize: '60px 60px' }} />
      
      {/* Header */}
      <header className="fixed top-0 left-0 right-0 z-50 bg-background-dark/80 backdrop-blur-md border-b border-primary/20 p-4 md:px-8 flex justify-between items-center">
        <div className="flex items-center gap-2">
          <span className="material-icons text-primary animate-pulse">terminal</span>
          <span className="text-lg font-display font-bold tracking-[0.3em] text-glow">HEFESTAI_OS</span>
        </div>
        <button 
          onClick={onLogin}
          className="group flex items-center gap-3 border border-primary/40 px-4 py-1.5 hover:bg-primary hover:text-black transition-all clip-tech-br"
        >
          <span className="text-[10px] font-bold uppercase tracking-widest">Acceso al Sistema</span>
          <span className="material-icons text-sm group-hover:scale-125 transition-transform">login</span>
        </button>
      </header>

      {/* Hero Section */}
      <main className="relative flex-1 pt-32 pb-20 px-6 max-w-7xl mx-auto w-full flex flex-col">
        <section className="text-center mb-32 relative">
          <div className="absolute -top-10 left-1/2 -translate-x-1/2 text-[8px] opacity-30 tracking-[1em] uppercase">INICIALIZANDO SECUENCIA_DE_ARRANQUE</div>
          <h1 className="text-6xl md:text-8xl font-display font-black mb-6 text-glow-primary animate-glitch-in uppercase leading-none">
            Forja tu <br/>
            <span className="bg-primary text-black px-4 ml-[-8px]">Leyenda</span> Galáctica
          </h1>
          <p className="max-w-2xl mx-auto text-primary/60 text-lg md:text-xl leading-relaxed font-mono">
            La interfaz retro-futurista definitiva para Maestros de Juego y Operativos. 
            Sintetiza mundos, despliega activos y archiva tu cronología a través de la nébula.
          </p>
          
          <div className="mt-12 flex flex-col sm:flex-row justify-center gap-4">
            <button 
              onClick={onSignup}
              className="px-8 py-4 bg-primary text-black font-bold uppercase tracking-widest hover:bg-white transition-all shadow-[0_0_30px_#25f46a44] clip-tech-tl"
            >
              Inicializar Nuevo Perfil
            </button>
            <button 
              onClick={onLogin}
              className="px-8 py-4 border border-primary text-primary font-bold uppercase tracking-widest hover:bg-primary/10 transition-all"
            >
              Verificar Credenciales
            </button>
          </div>
        </section>

        {/* 3-Column Features Section */}
        <section className="grid grid-cols-1 md:grid-cols-3 gap-8 mb-24">
          {/* Column 1 - Genesis Node */}
          <div className="bg-surface-dark/40 border border-primary/10 p-8 flex flex-col gap-4 group hover:border-primary/40 transition-all clip-tech-br backdrop-blur-sm">
            <div className="w-12 h-12 border border-primary/20 flex items-center justify-center bg-primary/5 mb-4 group-hover:bg-primary/20 transition-all shrink-0">
              <span className="material-icons text-3xl leading-none">hub</span>
            </div>
            <h3 className="text-xl font-display font-bold uppercase tracking-widest text-glow">Nodo Génesis</h3>
            <p className="text-sm text-primary/50 leading-relaxed font-mono">
              Despliega sistemas estelares de alta fidelidad y entidades biométricas. Nuestro enlace neural GenAI sintetiza órbitas planetarias únicas, rasgos de especies y perfiles operativos en tiempo real.
            </p>
            <div className="mt-auto pt-6 flex gap-1">
              {[...Array(5)].map((_, i) => <div key={i} className="w-1.5 h-1.5 bg-primary/20" />)}
            </div>
          </div>

          {/* Column 2 - Archive Core */}
          <div className="bg-surface-dark/40 border border-primary/10 p-8 flex flex-col gap-4 group hover:border-primary/40 transition-all clip-tech-br backdrop-blur-sm">
            <div className="w-12 h-12 border border-primary/20 flex items-center justify-center bg-primary/5 mb-4 group-hover:bg-primary/20 transition-all shrink-0">
              <span className="material-icons text-3xl leading-none">auto_awesome_motion</span>
            </div>
            <h3 className="text-xl font-display font-bold uppercase tracking-widest text-glow">Núcleo de Archivo</h3>
            <p className="text-sm text-primary/50 leading-relaxed font-mono">
              Repositorio centralizado para todos los activos operativos. Accede a la galería galáctica con fallas sincronizadas de CRT para una experiencia de recuperación de datos inmersiva.
            </p>
            <div className="mt-auto pt-6 flex gap-1">
              {[...Array(5)].map((_, i) => <div key={i} className="w-1.5 h-1.5 bg-primary/40" />)}
            </div>
          </div>

          {/* Column 3 - Tactical Layer */}
          <div className="bg-surface-dark/40 border border-primary/10 p-8 flex flex-col gap-4 group hover:border-primary/40 transition-all clip-tech-br backdrop-blur-sm">
            <div className="w-12 h-12 border border-primary/20 flex items-center justify-center bg-primary/5 mb-4 group-hover:bg-primary/20 transition-all shrink-0">
              <span className="material-icons text-3xl leading-none">front_hand</span>
            </div>
            <h3 className="text-xl font-display font-bold uppercase tracking-widest text-glow">Capa Táctica</h3>
            <p className="text-sm text-primary/50 leading-relaxed font-mono">
              Interactúa con nuestro lanzador de dados integrado basado en física. Simulado en un entorno de vacío 3D para asegurar cero interferencia gravitacional en los resultados de probabilidad.
            </p>
            <div className="mt-auto pt-6 flex gap-1">
              {[...Array(5)].map((_, i) => <div key={i} className="w-1.5 h-1.5 bg-primary/60" />)}
            </div>
          </div>
        </section>

        {/* Additional Terminal Decoration */}
        <section className="border-t border-primary/10 py-12 flex flex-col md:flex-row justify-between gap-8 opacity-40">
           <div className="font-mono text-[10px] space-y-1">
              <p>&gt; TIEMPO_ACTIVIDAD: 99.999%</p>
              <p>&gt; LATENCIA: 12ms [SERVIDOR_NEBULA_01]</p>
              <p>&gt; ENCRIPTACIÓN: ACTIVA</p>
           </div>
           <div className="flex gap-4 items-center">
              <div className="w-32 h-1 bg-primary/10 relative overflow-hidden">
                <div className="absolute inset-0 bg-primary/40 animate-[scan_2s_linear_infinite]" />
              </div>
              <span className="text-[10px] uppercase font-bold tracking-widest">Sincronizando Enlace Neural...</span>
           </div>
        </section>
      </main>

      {/* Footer */}
      <footer className="mt-auto border-t border-primary/20 p-12 bg-black/40 backdrop-blur-lg">
        <div className="max-w-7xl mx-auto grid grid-cols-1 md:grid-cols-2 gap-12 items-end">
          <div className="space-y-4">
            <div className="flex items-center gap-2 opacity-50">
              <span className="material-icons text-sm">security</span>
              <span className="text-[10px] uppercase font-bold tracking-[0.2em]">Protocolo de Privacidad de Datos</span>
            </div>
            <p className="text-[10px] text-primary/30 leading-relaxed uppercase max-w-md">
              Aviso: Hefestai_OS es una herramienta de simulación especializada. Todas las entidades generadas, configuraciones estelares y planos de vehículos son propiedad del perfil de Maestro de Juego respectivo. La sincronización del enlace neural está encriptada con AES-4096. No se utilizan créditos galácticos reales en esta simulación.
            </p>
          </div>
          <div className="md:text-right">
             <p className="text-[8px] text-primary/40 mb-2 uppercase tracking-widest">Estado del Sistema: [OPERATIVO]</p>
             <p className="text-[8px] text-primary/20 uppercase tracking-[0.5em]">&copy; 2077 OMEGA_SYSTEMS_GLOBAL</p>
          </div>
        </div>
      </footer>
    </div>
  );
};

export default Home;
