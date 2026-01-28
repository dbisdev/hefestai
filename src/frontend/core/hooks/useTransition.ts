/**
 * Screen Transition Hook
 * Single Responsibility: Handle screen navigation with animations
 */

import { useState, useCallback } from 'react';
import { TRANSITION_OUT_DURATION, TRANSITION_IN_DURATION } from '../config/constants';
import type { Screen, TransitionStage } from '../types';

interface UseTransitionResult {
  currentScreen: Screen;
  transitionStage: TransitionStage;
  navigate: (newScreen: Screen) => void;
  isTransitioning: boolean;
}

export function useTransition(initialScreen: Screen): UseTransitionResult {
  const [currentScreen, setCurrentScreen] = useState<Screen>(initialScreen);
  const [transitionStage, setTransitionStage] = useState<TransitionStage>('idle');

  const navigate = useCallback((newScreen: Screen) => {
    if (newScreen === currentScreen || transitionStage !== 'idle') return;

    setTransitionStage('out');
    
    setTimeout(() => {
      setCurrentScreen(newScreen);
      setTransitionStage('in');
      
      setTimeout(() => {
        setTransitionStage('idle');
      }, TRANSITION_IN_DURATION);
    }, TRANSITION_OUT_DURATION);
  }, [currentScreen, transitionStage]);

  return {
    currentScreen,
    transitionStage,
    navigate,
    isTransitioning: transitionStage !== 'idle',
  };
}
