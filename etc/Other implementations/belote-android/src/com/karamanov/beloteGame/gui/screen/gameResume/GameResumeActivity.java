package com.karamanov.beloteGame.gui.screen.gameResume;

import java.util.ArrayList;
import java.util.Locale;

import android.app.Activity;
import android.graphics.Bitmap;
import android.graphics.Color;
import android.graphics.Typeface;
import android.os.Bundle;
import android.util.TypedValue;
import android.view.Gravity;
import android.view.View;
import android.widget.ImageView;
import android.widget.LinearLayout;
import android.widget.RelativeLayout;
import android.widget.ScrollView;
import android.widget.TableLayout;
import android.widget.TableRow;
import android.widget.TableRow.LayoutParams;
import android.widget.TextView;
import belote.bean.Game;
import belote.bean.Player;
import belote.bean.Team;
import belote.bean.announce.Announce;
import belote.bean.announce.suit.AnnounceSuit;
import belote.bean.pack.card.suit.Suit;
import belote.bean.pack.card.suit.SuitIterator;
import belote.bean.pack.sequence.Sequence;
import belote.bean.pack.sequence.SequenceIterator;
import belote.bean.pack.sequence.SequenceList;
import belote.bean.pack.square.Square;
import belote.bean.pack.square.SquareIterator;
import belote.bean.pack.square.SquareList;
import belote.logic.HumanBeloteFacade;

import com.karamanov.beloteGame.Belote;
import com.karamanov.beloteGame.R;
import com.karamanov.beloteGame.gui.graphics.PictureDecorator;
import com.karamanov.beloteGame.text.PlayerNameDecorator;
import com.karamanov.beloteGame.text.Sentence;
import com.karamanov.beloteGame.text.TextDecorator;
import com.karamanov.framework.graphics.ImageUtil;
import com.karamanov.framework.message.Message;

public final class GameResumeActivity extends Activity {

    private boolean showWinner = false;
    
    private boolean isShowWinner = false;

    private final String showWinnerStr = "SHOW_WINNER";

    public GameResumeActivity() {
        super();
    }

    private Game game;

    /** Called when the activity is first created. */
    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        loadSavedValues(savedInstanceState);
        
        game = Belote.getBeloteFacade(this).getGame();

        if (game != null) {
            if (isShowWinner && game.getWinnerTeam() != null) {
                setFaceView();
                showWinner = false;
            } else {
                isShowWinner = false;
                showWinner = game.getWinnerTeam() != null;
                setContentView(consructContentView());
            }
        }
    }

    private void loadSavedValues(Bundle savedInstanceState) {
        if (savedInstanceState != null) {
            Boolean bool = savedInstanceState.getBoolean(showWinnerStr);
            if (bool != null) {
                isShowWinner = bool.booleanValue();
            }
        }
    }

    @Override
    protected void onSaveInstanceState(Bundle outState) {
        super.onSaveInstanceState(outState);

        outState.putBoolean(showWinnerStr, isShowWinner);
    }
    
    private View consructContentView() {
        int dip2 = Belote.fromPixelToDip(this, 2);
        int dip5 = Belote.fromPixelToDip(this, 5);

        ScrollView scroll = new ScrollView(this);
        LinearLayout vertical = new LinearLayout(this);
        vertical.setOrientation(LinearLayout.VERTICAL);
        RelativeLayout relative = new RelativeLayout(this);

        TextDecorator textDecorator = new TextDecorator(this);

        View contractView = consructContractView(textDecorator);
        RelativeLayout.LayoutParams params = new RelativeLayout.LayoutParams(LayoutParams.MATCH_PARENT, LayoutParams.WRAP_CONTENT);
        params.addRule(RelativeLayout.CENTER_HORIZONTAL);
        params.addRule(RelativeLayout.ALIGN_PARENT_TOP);
        params.topMargin = dip2;
        contractView.setLayoutParams(params);
        relative.addView(contractView);

        TableLayout table = getGameInfo(textDecorator);
        if (table != null) {
            params = new RelativeLayout.LayoutParams(LayoutParams.WRAP_CONTENT, LayoutParams.WRAP_CONTENT);
            params.addRule(RelativeLayout.CENTER_HORIZONTAL);
            params.addRule(RelativeLayout.CENTER_IN_PARENT);
            params.addRule(RelativeLayout.BELOW, contractView.getId());
            params.topMargin = dip5;
            table.setLayoutParams(params);
            relative.addView(table);
        }

        vertical.addView(relative);
        scroll.addView(vertical);
        scroll.setBackgroundResource(R.drawable.score_bkg);
        
        return scroll;
    }
    
    private View consructContractView(TextDecorator textDecorator) {
        LinearLayout contractView = new LinearLayout(this);
        contractView.setOrientation(LinearLayout.VERTICAL);
        contractView.setId(123);

        TextView contractTitle = new TextView(this);

        contractTitle.setText(getString(R.string.GameContract));
        contractTitle.setTypeface(Typeface.SERIF, Typeface.BOLD);
        contractTitle.setTextColor(Color.YELLOW);
        contractTitle.setGravity(Gravity.CENTER_HORIZONTAL);
        contractTitle.setTextSize(TypedValue.COMPLEX_UNIT_SP, 18);
        contractView.addView(contractTitle);

        final ArrayList<String> announceText = textDecorator.getAnnounceText(game.getAnnounceList());
        for (String announceLine : announceText) {
            TextView announceLineView = new TextView(this);
            announceLineView.setText(announceLine);
            announceLineView.setTypeface(Typeface.SERIF, Typeface.BOLD);
            announceLineView.setTextColor(Color.YELLOW);
            announceLineView.setGravity(Gravity.CENTER_HORIZONTAL);
            announceLineView.setTextSize(TypedValue.COMPLEX_UNIT_SP, 15);
            contractView.addView(announceLineView);
        }
        
        return contractView;
    }

    private TableLayout getGameInfo(TextDecorator textDecorator) {
        int dip4 = Belote.fromPixelToDip(this, 4);
        int dip5 = Belote.fromPixelToDip(this, 5);

        final Announce announce = game.getAnnounceList().getOpenContractAnnounce();
        if (announce != null) {
            final boolean ntAnnounce = announce.getAnnounceSuit().equals(AnnounceSuit.NotTrump);

            TableLayout table = new TableLayout(this);
            // Draw team player' sequences.

            for (int i = 0; i < game.getTeamsCount(); i++) {
                final Team team = game.getTeam(i);
                if (i == 1) {
                    TableRow fake = createFakeRow(2, dip4, dip4);
                    table.addView(fake);
                }

                TableRow caption = createCaptionRow(team);
                table.addView(caption);

                TableRow row = createCardsPointsRow(team);
                table.addView(row);

                if (team.getAnnouncePoints() > 0 && !ntAnnounce) {
                    row = createAnnouncePointsRow(team, textDecorator); 
                    table.addView(row);
                }

                if (team.getPointsInfo().getCouplesPoints() > 0) {
                    row = createCouplesRow(team);
                    table.addView(row);
                }

                if (team.getPointsInfo().getLastHand() > 0) {
                    row = createLastHandPoints(team);
                    table.addView(row);
                }

                if (team.getPointsInfo().getTotalPoints() >= 0) {
                    row = createTotalPoints(team);
                    table.addView(row);
                }

                if (team.getPointsInfo().getCapotPoints() > 0) {
                    row = createCapotPointsRow(team);
                    table.addView(row);
                    
                    row = createTotalWithCapotPointsRow(team);
                    table.addView(row);
                }
            }

            TableRow fake = createFakeRow(2, dip5, dip5);
            table.addView(fake);

            TableRow row = creatPointsDistributionCaptionRow();
            table.addView(row);

            for (int i = 0; i < game.getTeamsCount(); i++) {
                row = creatPointsDistributionTeamRow(game.getTeam(i));
                table.addView(row);
            }

            if (game.getHangedPoints() > 0) {
                row = createHangedPointsRow();
                table.addView(row);
            }
            
            return table;
        }

        return null;
    }
    
    private String getTeamCaption(Team team) {
        Sentence sentence = new Sentence(" - ");
        
        for (int i = 0; i < team.getPlayersCount(); i++) {
            PlayerNameDecorator player = new PlayerNameDecorator(team.getPlayer(i));
            sentence.addWord(player.decorate(this));
        }

        return sentence.toString();
    }
    
    private TableRow createHangedPointsRow() {
        int points = game.getHangedPoints();
        return createRow(getString(R.string.HangedPoints), String.valueOf(points));
    }

    private TableRow creatPointsDistributionTeamRow(Team team) {
        int points = 0;
        int size = team.getPoints().size();
        if (size > 0) {
            points = team.getPoints().getPointsAt(size - 1);
        }
        
        return createRow(getTeamCaption(team), String.valueOf(points));
    }

    private TableRow creatPointsDistributionCaptionRow() {
        return createRow(getString(R.string.PointsDistribution));
    }

    private TableRow createCapotPointsRow(Team team) {
        String capotPoints = String.valueOf(team.getPointsInfo().getCapotPoints() / 10);
        return createRow(getString(R.string.Capot), capotPoints);
    }
    
    private TableRow createTotalWithCapotPointsRow(Team team) {
        String name = getString(R.string.Total) + " " + getString(R.string.Points).toLowerCase(Locale.getDefault()) + "(" + getString(R.string.Capot) + ")";
        String totalPoints = String.valueOf(team.getPointsInfo().getTotalTrickPoints());
        String value = totalPoints + " (" + String.valueOf(team.getPointsInfo().getTotalPoints()) + ")";
 
        return createRow(name, value);
    }

    private TableRow createTotalPoints(Team team) {
        String name = getString(R.string.Total);
        
        int totalRound = team.getPointsInfo().getTotalTrickPoints();
        int capotRound = team.getPointsInfo().getCapotPoints() / 10;
        String totalRoundPoints = String.valueOf(totalRound - capotRound);

        int total = team.getPointsInfo().getTotalPoints();
        int capot = team.getPointsInfo().getCapotPoints();
        String totalPoints = String.valueOf(total - capot);
        
        String value = totalRoundPoints + " (" + totalPoints + ")";
        
        return createRow(name, value);
    }

    private TableRow createLastHandPoints(Team team) {
        String name = getString(R.string.LastHand);
        String value = String.valueOf(team.getPointsInfo().getLastHand());
        return createRow(name, value);
    }

    private TableRow createCardsPointsRow(Team team) {
        String name = getString(R.string.Cards);
        String value = String.valueOf(team.getPointsInfo().getCardPoints());
        return createRow(name, value);
    }

    private TableRow createCouplesRow(Team team) {
        LinearLayout horizontal = new LinearLayout(this);
        horizontal.setOrientation(LinearLayout.HORIZONTAL);

        String couplePoints = String.valueOf(team.getPointsInfo().getCouplesPoints());
        TextView value = new TextView(this);
        value.setText(couplePoints);
        value.setTextColor(Color.WHITE);
        LinearLayout.LayoutParams llp = new LinearLayout.LayoutParams(LayoutParams.WRAP_CONTENT, LayoutParams.WRAP_CONTENT);
        int dip10 = Belote.fromPixelToDip(this, 10);
        llp.rightMargin = dip10;
        value.setLayoutParams(llp);
        horizontal.addView(value);

        for (SuitIterator iterator = Suit.iterator(); iterator.hasNext();) {
            Suit suit = iterator.next();
            if (team.getCouples().hasCouple(suit)) {
                PictureDecorator pictureDecorator = new PictureDecorator(this);
                Bitmap suitImage = pictureDecorator.getSuitImage(suit);
                ImageView imageView = new ImageView(this);
                imageView.setImageBitmap(suitImage);
                
                LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(LayoutParams.WRAP_CONTENT, LayoutParams.WRAP_CONTENT);
                params.gravity = Gravity.BOTTOM;
                imageView.setLayoutParams(params);
                horizontal.addView(imageView);
            }
        }

        return createRow(getString(R.string.Couples), horizontal);
    }

    private TableRow createAnnouncePointsRow(Team team, TextDecorator textDecorator) {
        LinearLayout vertical = new LinearLayout(this);
        vertical.setOrientation(LinearLayout.VERTICAL);

        TextView value = new TextView(this);
        String announcePoints = String.valueOf(team.getPointsInfo().getAnnouncePoints());
        value.setText(announcePoints);
        value.setTextColor(Color.WHITE);

        vertical.addView(value);

        for (int i = 0; i < team.getPlayersCount(); i++) {

            SquareList equalCardsList = team.getPlayer(i).getCards().getSquaresList();
            for (SquareIterator iterator = equalCardsList.iterator(); iterator.hasNext();) {
                Square square = iterator.next();
                TextView squareView = new TextView(this);
                squareView.setText(textDecorator.getSquareText(square));
                vertical.addView(squareView);
            }
        }

        PictureDecorator pictureDecorator = new PictureDecorator(this);
        for (int i = 0; i < team.getPlayersCount(); i++) {
            SequenceList sequencesList = team.getPlayer(i).getCards().getSequencesList();
            for (SequenceIterator iterator = sequencesList.iterator(); iterator.hasNext();) {
                Sequence sequence = iterator.next();
                Suit suit = sequence.getMaxCard().getSuit();

                String strLeft = String.valueOf(sequence.getType().getSequencePoints());
                strLeft += " (" + textDecorator.getRankSign(sequence.getMaxCard().getRank());
                String strRight = ")";
               
                Bitmap suitImage;
                if (team.getPointsInfo().getAnnouncePoints() == 0) {
                    suitImage = ImageUtil.transformToDisabledImage(pictureDecorator.getSuitImage(suit));
                } else {
                    suitImage = pictureDecorator.getSuitImage(suit);
                }

                LinearLayout horizontal = new LinearLayout(this);
                horizontal.setOrientation(LinearLayout.HORIZONTAL);

                TextView leftView = new TextView(this);
                leftView.setText(strLeft);
                leftView.setTextColor(team.getPointsInfo().getAnnouncePoints() == 0 ? Color.LTGRAY : Color.WHITE);
                horizontal.addView(leftView);

                ImageView image = new ImageView(this);
                image.setImageBitmap(suitImage);
                
                LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(LayoutParams.WRAP_CONTENT, LayoutParams.WRAP_CONTENT);
                params.gravity = Gravity.BOTTOM;
                image.setLayoutParams(params);
                
                horizontal.addView(image);

                TextView rightView = new TextView(this);
                rightView.setText(strRight);
                rightView.setTextColor(team.getPointsInfo().getAnnouncePoints() == 0 ? Color.LTGRAY : Color.WHITE);
                horizontal.addView(rightView);

                vertical.addView(horizontal);
            }
        }

        return createRow(getString(R.string.Announce), vertical);
    }
    
    private TableRow createCaptionRow(Team team) {
        return createRow(getTeamCaption(team));
    }

    private TableRow createFakeRow(int span, int topMargin, int bottomMargin) {
        TableRow row = new TableRow(this);
        TextView empty = new TextView(this);
        TableRow.LayoutParams params = new TableRow.LayoutParams();
        params.span = span;
        empty.setLayoutParams(params);
        row.addView(empty);

        TableLayout.LayoutParams tableParams = new TableLayout.LayoutParams();
        tableParams.topMargin = topMargin;
        tableParams.bottomMargin = bottomMargin;
        row.setLayoutParams(tableParams);
        
        return row;
    }

    private void setFaceView() {
        isShowWinner = true;
        int dip10 = Belote.fromPixelToDip(this, 10);

        LinearLayout vertical = new LinearLayout(this);
        vertical.setOrientation(LinearLayout.VERTICAL);

        ImageView image = new ImageView(this);

        String message;
        Player human = game.getPlayer(HumanBeloteFacade.HUMAN_PLAYER_INDEX);
        if (human.getTeam().equals(game.getWinnerTeam())) {
            image.setBackgroundResource(R.drawable.happy);
            message = getString(R.string.TeamWinsGame);
        } else {
            image.setBackgroundResource(R.drawable.unhappy);
            message = getString(R.string.TeamLostGame);
        }

        LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(LayoutParams.WRAP_CONTENT, LayoutParams.WRAP_CONTENT);
        params.gravity = Gravity.CENTER_HORIZONTAL;
        params.topMargin = dip10;
        params.bottomMargin = dip10;

        image.setLayoutParams(params);

        vertical.addView(image);

        TextView name = new TextView(this);
        name.setTextColor(Color.YELLOW);
        name.setTypeface(Typeface.SERIF, Typeface.BOLD);
        name.setTextSize(TypedValue.COMPLEX_UNIT_SP, 18);
        name.setText(message);

        params = new LinearLayout.LayoutParams(LayoutParams.WRAP_CONTENT, LayoutParams.WRAP_CONTENT);
        params.gravity = Gravity.CENTER_HORIZONTAL;
        params.topMargin = dip10;
        params.bottomMargin = dip10;
        name.setLayoutParams(params);
        vertical.addView(name);

        LinearLayout horizontal = new LinearLayout(this);
        horizontal.setOrientation(LinearLayout.HORIZONTAL);

        int orange = Color.rgb(255, 128, 64);

        TextView team0 = new TextView(this);
        if (game.getWinnerTeam().equals(game.getTeam(0))) {
            team0.setTextColor(orange);
        } else {
            team0.setTextColor(Color.YELLOW);
        }
        team0.setTypeface(Typeface.SERIF, Typeface.BOLD);
        team0.setTextSize(TypedValue.COMPLEX_UNIT_SP, 16);
        team0.setText(String.valueOf(game.getTeam(0).getPoints().getAllPoints()));
        horizontal.addView(team0);

        TextView sep = new TextView(this);
        sep.setTextColor(Color.YELLOW);
        sep.setTypeface(Typeface.SERIF, Typeface.BOLD);
        sep.setTextSize(TypedValue.COMPLEX_UNIT_SP, 16);
        sep.setText(" : ");
        horizontal.addView(sep);

        TextView team1 = new TextView(this);
        if (game.getWinnerTeam().equals(game.getTeam(1))) {
            team1.setTextColor(orange);
        } else {
            team1.setTextColor(Color.YELLOW);
        }
        team1.setTypeface(Typeface.SERIF, Typeface.BOLD);
        team1.setTextSize(TypedValue.COMPLEX_UNIT_SP, 16);
        team1.setText(String.valueOf(game.getTeam(1).getPoints().getAllPoints()));
        horizontal.addView(team1);

        params = new LinearLayout.LayoutParams(LayoutParams.WRAP_CONTENT, LayoutParams.WRAP_CONTENT);
        params.gravity = Gravity.CENTER_HORIZONTAL;
        params.topMargin = dip10;
        params.bottomMargin = dip10;
        horizontal.setLayoutParams(params);
        vertical.addView(horizontal);

        RelativeLayout relative = new RelativeLayout(this);
        relative.setBackgroundResource(R.drawable.score_bkg);

        RelativeLayout.LayoutParams rp = new RelativeLayout.LayoutParams(LayoutParams.WRAP_CONTENT, LayoutParams.WRAP_CONTENT);
        rp.addRule(RelativeLayout.CENTER_HORIZONTAL);
        rp.addRule(RelativeLayout.CENTER_VERTICAL);
        rp.addRule(RelativeLayout.CENTER_IN_PARENT);
        vertical.setLayoutParams(rp);

        relative.addView(vertical);

        setContentView(relative);
    }

    @Override
    public void onBackPressed() {
        if (game != null && showWinner && game.getWinnerTeam() != null) {
            setFaceView();
        } else {
            if (getApplication() instanceof Belote) {
                Belote belote = (Belote) getApplication();
                Message message = new Message(Belote.MT_CLOSE_END_GAME);
                belote.getMessageProcessor().sendMessage(message, true);
            }
            super.onBackPressed();
        }

        showWinner = false;
    }
    
    private TableRow createRow(String caption) {
        TableRow row = new TableRow(this);
        TextView captionText = new TextView(this);
        TableRow.LayoutParams rowParams = new TableRow.LayoutParams();
        rowParams.span = 2;
        rowParams.weight = 1;
        captionText.setLayoutParams(rowParams);
        captionText.setSingleLine();

        captionText.setText(caption);
        captionText.setGravity(Gravity.CENTER_HORIZONTAL);
        captionText.setTextColor(Color.BLACK);
        captionText.setTypeface(Typeface.DEFAULT_BOLD);
        row.addView(captionText);

        TableLayout.LayoutParams tableParams = new TableLayout.LayoutParams();
        int dip2 = Belote.fromPixelToDip(this, 2);
        tableParams.bottomMargin = dip2;
        row.setLayoutParams(tableParams);
        row.setBackgroundColor(Color.LTGRAY);

        return row;
    }
        
    private TableRow createRow(String name, String value) {
        TextView valueView = new TextView(this);
        valueView.setText(value);
        valueView.setTextColor(Color.WHITE);
        
        return createRow(name, valueView);
    }
    
    private TableRow createRow(String name, View valueView) {
        int dip2 = Belote.fromPixelToDip(this, 2);
        int dip10 = Belote.fromPixelToDip(this, 10);
        
        TableRow row = new TableRow(this);
        
        TextView nameView = new TextView(this);
        nameView.setText(name);
        nameView.setTextColor(Color.WHITE);
        
        row.addView(nameView);

        TableRow.LayoutParams rowParams = new TableRow.LayoutParams();
        rowParams.weight = 1;
        rowParams.leftMargin = dip10;
        valueView.setLayoutParams(rowParams);
        
        row.addView(valueView);

        TableLayout.LayoutParams tableParams = new TableLayout.LayoutParams();
        tableParams.bottomMargin = dip2;
        row.setLayoutParams(tableParams);
        row.setBackgroundColor(Color.DKGRAY);
        row.setPadding(dip2, 0, dip2, 0);
        
        return row;
    }
}
