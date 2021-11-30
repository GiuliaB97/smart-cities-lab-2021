package it.unibo.scl.esnfc.completed;

import java.io.IOException;

import org.apache.commons.codec.binary.Hex;

import android.app.Activity;
import android.app.AlertDialog;
import android.app.PendingIntent;
import android.content.DialogInterface;
import android.content.Intent;
import android.nfc.NfcAdapter;
import android.nfc.Tag;
import android.nfc.tech.MifareClassic;
import android.nfc.tech.MifareUltralight;
import android.os.Bundle;
import android.provider.Settings;
import android.util.Log;
import android.view.View;
import android.widget.TextView;

public class MainActivity extends Activity {
	
    NfcAdapter nfcAdapter;
    PendingIntent pendingIntent;
    
    StringBuilder memContent;

	
	TextView nfcId;
	TextView nfcTechSpec;
	TextView nfcInfo;
	TextView nfcMemContent;


	@Override
	protected void onCreate(Bundle savedInstanceState) {
		super.onCreate(savedInstanceState);
		setContentView(R.layout.activity_main);
		nfcId = (TextView) findViewById(R.id.nfc_id_value);
		nfcTechSpec = (TextView) findViewById(R.id.nfc_tech_spec_value);
		nfcInfo = (TextView) findViewById(R.id.nfc_info_value);
		nfcMemContent = (TextView) findViewById(R.id.nfc_memory_label);
		
		 memContent = new StringBuilder();
		
		nfcAdapter = NfcAdapter.getDefaultAdapter(this);
        if (nfcAdapter == null) {
        	new AlertDialog.Builder(this).setTitle("Attenzione").setMessage("Modulo NFC non presente nel dispositivo.").setPositiveButton("OK", null).create().show();
            finish();
            return;
        }
        

        pendingIntent = PendingIntent.getActivity(this, 0,
                new Intent(this, getClass()).addFlags(Intent.FLAG_ACTIVITY_SINGLE_TOP), 0);
	}
	

	    @Override
	    protected void onPause() {
	        super.onPause();
	        if (nfcAdapter != null) {
	            nfcAdapter.disableForegroundDispatch(this);
	        }
	    }


	public void start(View v) {
		if (nfcAdapter != null) {
            if (!nfcAdapter.isEnabled()) {
                showWirelessSettingsDialog();
            }
            nfcAdapter.enableForegroundDispatch(this, pendingIntent, null, null);
        }
	}

	public void stop(View v) {
		if (nfcAdapter != null) {
			nfcAdapter.disableForegroundDispatch(this);
		}
	}
	
	private void showWirelessSettingsDialog() {
	        AlertDialog.Builder builder = new AlertDialog.Builder(this);
	        builder.setMessage("Abilitare chip NFC.");
	        builder.setPositiveButton(android.R.string.ok, new DialogInterface.OnClickListener() {
	            public void onClick(DialogInterface dialogInterface, int i) {
	                Intent intent = new Intent(Settings.ACTION_WIRELESS_SETTINGS);
	                startActivity(intent);
	            }
	        });
	        builder.setNegativeButton(android.R.string.cancel, new DialogInterface.OnClickListener() {
	            public void onClick(DialogInterface dialogInterface, int i) {
	                finish();
	            }
	        });
	        builder.create().show();
	        return;
	}
	    
	
	@Override
    public void onNewIntent(Intent intent) {
        setIntent(intent);
        resolveIntent(intent);
    }
	
	private void resolveIntent(Intent intent) {
		String action = intent.getAction();
		if (NfcAdapter.ACTION_TAG_DISCOVERED.equals(action)
				|| NfcAdapter.ACTION_TECH_DISCOVERED.equals(action)
				|| NfcAdapter.ACTION_NDEF_DISCOVERED.equals(action)) {

			byte[] id = intent.getByteArrayExtra(NfcAdapter.EXTRA_ID);
			Tag tag = intent.getParcelableExtra(NfcAdapter.EXTRA_TAG);
			printId(tag.getId());
			printTechList(tag.getTechList());
			printTagInfo(tag);
		}
	}
	
	public void printId(byte[] data){
		nfcId.setText(byte2Hex(data));
	}
	
	
	private String byte2Hex(byte[] data){
		String hexString = new String (Hex.encodeHex(data)).toUpperCase();
		String res = "";
		for (int i = 0; i < hexString.length(); i+=2) {
			res += hexString.substring(i, i+2) +":";
		}
		return res.substring(0, res.length()-1);
	}
	
	public void printTechList(String[] data){
		StringBuffer stringBuffer = new StringBuffer();
		String prefix = "android.nfc.tech.";
	     for (String tech : data) {
	    	 stringBuffer.append(tech.substring(prefix.length()));
	    	 stringBuffer.append(", ");
	     }
	     nfcTechSpec.setText(stringBuffer.toString().substring(0, stringBuffer.length() - 2));
	}
	
	public void printTagInfo(Tag tag){
		StringBuffer stringBuffer = new StringBuffer();
		if(MifareClassic.get(tag) != null){
			final MifareClassic tagChip = MifareClassic.get(tag);
			
			nfcInfo.setText(MifareClassic.class.getSimpleName());
			stringBuffer.append("Tipo " + MifareClassic.class.getSimpleName());
			stringBuffer.append('\n');

			stringBuffer.append("Mifare size: ");
			stringBuffer.append(tagChip.getSize() + " bytes");
			stringBuffer.append('\n');

			stringBuffer.append("Mifare sectors: ");
			stringBuffer.append(tagChip.getSectorCount());
			stringBuffer.append('\n');

			stringBuffer.append("Mifare blocks: ");
			stringBuffer.append(tagChip.getBlockCount());

			new Thread(new Runnable() {
				
				@Override
				public void run() {
					readFromMifareClassic(tagChip);
				}
			}).start();
			
			
		}else if(MifareUltralight.get(tag) != null){
			final MifareUltralight tagChip = MifareUltralight.get(tag);
			nfcInfo.setText(MifareUltralight.class.getSimpleName());
			
			 String type = "Sconosciuto";
             switch (tagChip.getType()) {
             case MifareUltralight.TYPE_ULTRALIGHT:
                 type = "Ultralight";
                 break;
             case MifareUltralight.TYPE_ULTRALIGHT_C:
                 type = "Ultralight C";
                 break;
             }
             stringBuffer.append("Mifare Ultralight type: ");
             stringBuffer.append(type);
             
             new Thread(new Runnable() {
     			
     			@Override
     			public void run() {
     				readFromMifareUltralight(tagChip);
     			}
     		}).start();
             
		}else{
			stringBuffer.append("Informazioni non disponibili.");
		}
		
		nfcInfo.setText(stringBuffer.toString());
		
		
	}
	
	public void printMemContent(final String data){
		runOnUiThread(new Runnable() {
			@Override
			public void run() {
				nfcMemContent.setText(data);
				
			}
		});
	}
	
	private void readFromMifareClassic(MifareClassic tagChip){

		try {
			
			tagChip.connect();

			int sectors = tagChip.getSectorCount();
			
			for (int currentSector = 0; currentSector < sectors; currentSector++) {
				boolean auth = tagChip.authenticateSectorWithKeyA(currentSector, MifareClassic.KEY_DEFAULT);
	            if(auth){
	            	int blockOfSector = tagChip.getBlockCountInSector(currentSector);
	            	for (int currentBlock = 0; currentBlock < blockOfSector; currentBlock++) {
						//Lettura settore sec e Blocco i
						int firstBlockofSector =  tagChip.sectorToBlock(currentSector);
						tagChip.readBlock(firstBlockofSector + currentBlock);
					}
	            }
			}

			printMemContent(memContent.toString());

		} catch (Exception e) {
			Log.e(MainActivity.class.getSimpleName(),
					"IOException while reading MifareClassic message...",
					e);
		} finally {
			if (tagChip != null) {
				try {
					tagChip.close();
				} catch (IOException e) {
					Log.e(MainActivity.class.getSimpleName(),
							"Error closing tag...", e);
				}
			}
		}
	}
	
	
	public void readFromMifareUltralight(MifareUltralight tag){
        try {
            tag.connect();
            if(tag.getType() == MifareUltralight.TYPE_ULTRALIGHT){
            	//16 pagine, lettura di 4 pagine per volta (16 byte)
            	for(int i = 0; i < 4; i++){
            		byte[] payload = tag.readPages(i);
            		memContent.append(String.format("Page from %s to %s\n", i*4, (i*4)+3));
        			memContent.append(byte2Hex(payload));
        			memContent.append("\n");
            	}
            }
            if(tag.getType() == MifareUltralight.TYPE_ULTRALIGHT_C){
            	//48 pagine, lettura di 4 pagine per volta (16 byte)
            	for(int i = 0; i < 12; i++){
            		byte[] payload = tag.readPages(i);
                    memContent.append(String.format("Page from %s to %s\n", i*4, (i*4)+3));
        			memContent.append(byte2Hex(payload));
        			memContent.append("\n");
            	}
            }
            printMemContent(memContent.toString());
            //return new String(payload, Charset.forName("US-ASCII"));
        } catch (Exception e) {
            Log.e(MainActivity.class.getSimpleName(), "Exception while reading MifareUltralight memory...", e);
        } finally {
            if (tag != null) {
               try {
                   tag.close();
               }
               catch (IOException e) {
                   Log.e(MainActivity.class.getSimpleName(), "Error closing MifareUltralight tag...", e);
               }
            }
        }
    }
}
