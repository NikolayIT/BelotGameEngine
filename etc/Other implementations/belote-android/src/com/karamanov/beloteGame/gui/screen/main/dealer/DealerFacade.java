package com.karamanov.beloteGame.gui.screen.main.dealer;

import android.view.View;
import belote.logic.HumanBeloteFacade;

import com.karamanov.beloteGame.Belote;
import com.karamanov.beloteGame.gui.screen.main.BeloteView;
import com.karamanov.framework.MessageActivity;

public final class DealerFacade {

    private final MessageActivity context;

    private HumanBeloteFacade beloteFacade;
    
    private final BaseDealer announceDealer;
    
    private final BaseDealer playDealer;

    public DealerFacade(MessageActivity context, BeloteView belotPanel, View buttons) {
        this.context = context;
        this.beloteFacade = Belote.getBeloteFacade(context);

        announceDealer = new AnnounceDealer(context, belotPanel, buttons);
        playDealer = new PlayDealer(context, belotPanel, buttons);
    }
    
    private BaseDealer getDealer() {
        return beloteFacade.isAnnounceGameMode() ? announceDealer : playDealer;
    }

    /**
     * Checks pointer click.
     * @param x position.
     * @param y position.
     */
    public void checkPointerPressed(float x, float y) {
        getDealer().checkPointerPressed(x, y);
    }

    /**
     * Checks key click.
     * 
     * @param keyCode pressed key code.
     * @param gameAction status.
     */
    public void checkKeyPressed(int keyCode) {
        getDealer().checkKeyPressed(keyCode);
    }

    public void invalidateGame() {
        getDealer().invalidateGame(0);
    }

    public void invalidateGame(int delay) {
        getDealer().invalidateGame(delay);
    }
    
    public final int getUpperCardY() {
        return getDealer().getUpperCardY();
    }

    public void onExit() {
        Belote.terminate(context);
        context.finish();
    }

    /**
     * On surface change (reposition message screen if is visible)
     */
    public void onSurfaceChanged() {
        getDealer().onSurfaceChanged();
    }

    /**
     * Called when end game activity is closed.
     */
    public void onCloseEndGame() {
        getDealer().onCloseEndGame();
        Belote belote = (Belote) context.getApplication();
        belote.getMessageProcessor().runMessaging();
    }
}