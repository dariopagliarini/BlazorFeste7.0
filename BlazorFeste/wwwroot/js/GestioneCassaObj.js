﻿"use strict";

export var GestioneCassaObj = {
  objRef: null,

  confirmPopup: null,
  toolbarCassa: null,
  toolbarScontrino: null,
  gridOrdini: null,
  store: null,
  Prodotti: null,
  arrayTavoli: null,
  arrayCoperti: null,
  scontrino_data: null,
  scontrino_store: null,
  idUltimoOrdine: null,

  cbStampaAbilitata: null,
  cbChiediConferma: null,

  btnRistampaOrdine: null,

  btnServito: null,
  btnBanco: null,
  btnCancellaOrdine: null,

  flagStampaAbilitata: true,
  flagChiediConferma: false,

  edtTavolo: "",
  edtCoperti: "",
  edtReferente: "",
  edtNote: "",

  init: (_objRef, _Cassa) => {
    console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneCassaObj - init - Ordini: " + _Cassa.ordiniDellaCassa + " (" + _Cassa.idUltimoOrdine + ")");

    document.addEventListener('keydown', GestioneCassaObj.keyHandler);

    GestioneCassaObj.objRef = _objRef;
    GestioneCassaObj.idUltimoOrdine = _Cassa.idUltimoOrdine;

    GestioneCassaObj.flagStampaAbilitata = _Cassa.scontrinoAbilitato;
    console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneCassaObj - init - flagStampaAbilitata: " + GestioneCassaObj.flagStampaAbilitata);

    GestioneCassaObj.arrayTavoli = [];
    for (var i = 1; i <= 120; i++) {
      GestioneCassaObj.arrayTavoli.push(i);
    }
    GestioneCassaObj.arrayCoperti = [];
    for (var i = 1; i <= 32; i++) {
      GestioneCassaObj.arrayCoperti.push(i);
    }

    $("#confirmPopup").dxPopup({
      contentTemplate: function () { return $('<div>').append($("<p>Cosa devo stampare?</p>")); },
      width: 300,
      height: 160,
      showTitle: true,
      title: 'Confermare la richiesta',
      visible: false,
      dragEnabled: false,
      hideOnOutsideClick: true,
      showCloseButton: true,
      position: {
        at: 'center',
        my: 'center',
      },
      toolbarItems: [{
        widget: 'dxButton',
        toolbar: 'bottom',
        location: 'center',
        options: {
          icon: 'user',
          text: 'Cassa',
          onClick() {
            _objRef.invokeMethodAsync("OnPrintRequest_Consumi", false)
              .catch(err => console.error(err.toString()));
            GestioneCassaObj.confirmPopup.hide();
          },
        },
      },
      {
        widget: 'dxButton',
        toolbar: 'bottom',
        location: 'center',
        options: {
          icon: 'runner',
          text: 'Globale',
          onClick() {
            _objRef.invokeMethodAsync("OnPrintRequest_Consumi", true)
              .catch(err => console.error(err.toString()));
            GestioneCassaObj.confirmPopup.hide();
          },
        },
      }
      ],
      onInitialized(e) {
        GestioneCassaObj.confirmPopup = e.component;
      },
    });

    GestioneCassaObj.scontrino_data = [];
    GestioneCassaObj.scontrino_store = new DevExpress.data.ArrayStore({
      key: "idProdotto",
      data: GestioneCassaObj.scontrino_data,
      onInserted: function (values, key) {
        GestioneCassaObj.gridScontrino.refresh();
        GestioneCassaObj.updateButtonState();
        GestioneCassaObj.calcolaResto();
      },
      onUpdated: function (key, values) {
        GestioneCassaObj.gridScontrino.refresh();
        GestioneCassaObj.updateButtonState();
        GestioneCassaObj.calcolaResto();
      },
      onRemoved: function (key) {
        GestioneCassaObj.gridScontrino.refresh();
        GestioneCassaObj.updateButtonState();
        GestioneCassaObj.calcolaResto();
      }
    });

    $('#toolbarCassa').dxToolbar({
      items: [
        {
          location: 'before',
          widget: 'dxButton',
          visible: _Cassa.scontrinoAbilitato,
          options: {
            icon: 'print',
            onClick(e) {
              var result = DevExpress.ui.dialog.confirm("<i>Confermi l'operazione?</i>", "Scontrino di Prova");
              result.done(function (dialogResult) {
                if (dialogResult) {
                  e.component.option("visible", false);

                  _objRef.invokeMethodAsync("OnPrintRequest_StampaDiProva")
                    .catch(err => console.error(err.toString()));
                }
              });
            },
          },
        },
        {
          location: 'before',
          locateInMenu: 'never',
          template() {
            return $("<div class='toolbar-label' style='padding-right:12px'><b>Gestione " + _Cassa.cassa + "</b></div>");
          },
        },
        {
          location: 'before',
          widget: 'dxCheckBox',
          visible: _Cassa.scontrinoAbilitato,
          options: {
            value: GestioneCassaObj.flagStampaAbilitata,
            text: "Stampa Scontrino",

            onInitialized: function (e) {
              GestioneCassaObj.cbStampaAbilitata = e.component;
            },
            
            onValueChanged: function (e) {
              GestioneCassaObj.flagStampaAbilitata = e.value;

              GestioneCassaObj.btnRistampaOrdine.option("disabled", !e.value);
            }
          },
        },
        {
          location: 'before',
          widget: 'dxCheckBox',
          options: {
            value: GestioneCassaObj.flagChiediConferma,
            text: "Chiedi Conferma",

            onInitialized: function (e) {
              GestioneCassaObj.cbChiediConferma = e.component;
            },

            onValueChanged: function (e) {
              GestioneCassaObj.flagChiediConferma = e.value;
            }
          },
        },
        {
          location: 'after',
          widget: 'dxButton',
          visible: _Cassa.scontrinoAbilitato,
          options: {
            disabled: (GestioneCassaObj.idUltimoOrdine === 0),
            icon: 'fa-solid fa-print',
            type: 'default',
            text: 'Ristampa',
            hint: "Ristampa Ultimo Ordine",
            
            onInitialized: function (e) {
              GestioneCassaObj.btnRistampaOrdine = e.component;
            },

            onClick() {
              var result = DevExpress.ui.dialog.confirm("<i>Confermi l'operazione?</i>", "Ristampa Scontrino");
              result.done(function (dialogResult) {
                if (dialogResult) {
                  _objRef.invokeMethodAsync("OnPrintRequest",
                    GestioneCassaObj.flagStampaAbilitata,
                    GestioneCassaObj.idUltimoOrdine)
                    .catch(err => console.error(err.toString()));
                }
              });
            },
          },
        },
        {
          location: 'after',
          widget: 'dxButton',
          visible: _Cassa.scontrinoAbilitato,
          options: {
            elementAttr: { id: 'btnStampaConsumi' },
            disabled: false,
            icon: 'fa-solid fa-print',
            type: 'default',
            text: 'Consumi',
            hint: "Stampa Consumi",
            onClick() {
              GestioneCassaObj.confirmPopup.show();
            },
          },
        },
        {
          location: 'after',
          locateInMenu: 'never',
          template() {
            return $("<div id='lblDataOra' class='toolbar-label'>-</div>");
          },
        },
      ],
      onInitialized(e) {
        GestioneCassaObj.toolbarCassa = e.component;
      },
    });

    $('#toolbarScontrino').dxToolbar({
      items: [
        {
          location: 'before',
          widget: 'dxButton',
          options: {
            width: 65,
            height: 40,
            type: 'giallo',
            text: 'CANCELLA ORDINE',
            hint: "Azzera Dati Ordine",
            onInitialized(e) {
              GestioneCassaObj.btnCancellaOrdine = e.component;
            },
            onClick(e) {
              var result = DevExpress.ui.dialog.confirm("<i>Sei proprio sicuro/a?</i>", "Cancella Ordine");
              result.done(function (dialogResult) {
                if (dialogResult) {
                  GestioneCassaObj.btnCancellaOrdineOnClick();
                }
              });
            },
          },
        },
        {
          location: 'after',
          widget: 'dxNumberBox',
          options: {
            label: "Contanti [F8]",
            stylingMode: "filled",
            format: { type: "currency", currency: "EUR", precision: 2 },
            min: 0,
            mode: "number",
            step: 5,
            width: 90,
            height: 40,
            elementAttr: {
              id: 'edtContanti',
              style: "margin-top: 0px; background-color: ivory"
            },
            inputAttr: {
              style: "font-size: 18px"
            },
            onValueChanged(data) {
              GestioneCassaObj.calcolaResto();
            },
          },
        },
        {
          location: 'after',
          widget: 'dxNumberBox',
          options: {
            label: "Resto",
            stylingMode: "filled",
            format: { type: "currency", currency: "EUR", precision: 2 },
            readOnly: true,
            width: 90,
            height: 40,
            elementAttr: {
              id: 'edtResto',
              style: "font-weight: bold; margin-top: 0px; background-color: ivory"
            },
            inputAttr: {
              style: "font-size: 18px; font-weight: bold"
            },
          },
        },
        {
          location: 'after',
          widget: 'dxButton',
          visible: _Cassa.soloBanco == false,
          options: {
            width: 70,
            height: 40,
            type: 'verde',
            text: 'SERVITO AL TAVOLO',
            hint: "Ordine Servito al tavolo",

            onInitialized(e) {
              GestioneCassaObj.btnServito = e.component;
            },

            onClick(e) {
              if (GestioneCassaObj.flagChiediConferma) {
                var result = DevExpress.ui.dialog.confirm("<i>Confermi l'operazione?</i>", "Chiusura Ordine");
                result.done(function (dialogResult) {
                  if (dialogResult) {
                    GestioneCassaObj.btnSaveToMySQL('SERVITO');
                  }
                });
              } else {
                GestioneCassaObj.btnSaveToMySQL('SERVITO');
              }
            },
          },
        },
        {
          location: 'after',
          widget: 'dxButton',
          options: {
            width: 70,
            height: 40,
            type: 'verde',
            text: 'RITIRO AL BANCO',
            hint: "Ordine Ritirato al Banco",
            onInitialized(e) {
              GestioneCassaObj.btnBanco = e.component;
            },
            onClick(e) {
              if (GestioneCassaObj.flagChiediConferma) {
                var result = DevExpress.ui.dialog.confirm("<i>Confermi l'operazione?</i>", "Chiusura Ordine");
                result.done(function (dialogResult) {
                  if (dialogResult) {
                    GestioneCassaObj.btnSaveToMySQL('BANCO');
                  }
                });
              } else {
                GestioneCassaObj.btnSaveToMySQL('BANCO');
              }
            },
          },
        },
      ],
      onInitialized(e) {
        GestioneCassaObj.toolbarScontrino = e.component;
      },
    });

    $("#gridScontrino").dxDataGrid({
      dataSource: GestioneCassaObj.scontrino_store,
      showColumnHeaders: true,
      height: "100%",
      width: "100%",
      columns: [
        {
          caption: "#", dataField: "idProdotto", alignment: "center", width: 30, visible: false, sortIndex: 0, sortOrder: "asc"
        },
        {
          caption: "Prodotto",
          dataField: "nomeProdotto", alignment: "left",
          cellTemplate: function (element, info) {
            $("<div style='font-size:13px'>" + info.value + "</div>").appendTo(element);
          },
        },
        {
          caption: "Qtà",
          dataField: "quantitàProdotto",
          width: 50,
          alignment: "center",
          cellTemplate: function (element, info) {
            $('<span class="quantity-value">' + info.value + '</span>')
              .appendTo(element);
          },
        },
        {
          caption: "Azioni",
          width: 85,
          type: "buttons",
          buttons: [{
              icon: "minus",
              hint: "Decrementa quantità",
              onClick: function (e) {
                GestioneCassaObj.minusClick("decr", e.row.data);
              }
            },
            {
              icon: "trash",
              hint: "Elimina riga dall'ordine",
              onClick: function (e) {
                GestioneCassaObj.minusClick("delete", e.row.data);
              }
            },
            {
              icon: "plus",
              hint: "Incrementa quantità",
              onClick: function (e) {
                GestioneCassaObj.minusClick("incr", e.row.data);
              }
            }
          ]
        },
        {
          caption: "Totale (€)",
          dataField: "totale",
          format: {
            type: "fixedPoint",
            precision: 2
          },
          width: 85,
          alignment: "right",
          cellTemplate: function (element, info) {
            $("<div class='total-value'>" + info.text + "</div>").appendTo(element);
          },
        },
      ],
      summary: {
        totalItems:
          [{
            column: 'nomeProdotto',
            summaryType: 'count',
            alignment: "right",
            customizeText(data) {
                return "Totale:";
              },
            },
            { column: 'quantitàProdotto', summaryType: 'sum', valueFormat: { type: "fixedPoint", precision: 0 }, displayFormat: "{0}" },
            { column: 'totale', summaryType: 'sum', valueFormat: { type: "fixedPoint", precision: 2 }, displayFormat: "{0}" },
          ],
      },
      noDataText: "Selezionare un prodotto con il mouse per iniziare un nuovo ordine. Quando l'ordine è concluso cliccare \"SERVITO AL TAVOLO\" o \"RITIRO AL BANCO\" per chiuderlo e stamparlo. Nel caso di ordine \"SERVITO AL TAVOLO\" ricordate di inserire anche i dati relativi al \"Tavolo\" ed al n° dei \"Coperti\". Il campo \"Signor\" dovrebbe essere compilato in ogni caso.",
      showRowLines: true,
      rowAlternationEnabled: true,
      columnAutoWidth: true,
      wordWrapEnabled: true,
      showBorders: true,
      hoverStateEnabled: true,
      paging: { enabled: false },
      scrolling: { mode: "standard" },
      selection: { mode: 'single', },
      sorting: { mode: "none" },
      onKeyDown: function(e) {
        switch (e.event.key) {
          case "-":
            var selectedData = e.component.getSelectedRowsData();
            if (selectedData.length > 0) {
              GestioneCassaObj.minusClick("decr", selectedData[0]);
            }
            e.handled = true;
            break;

          case "+":
            var selectedData = e.component.getSelectedRowsData();
            if (selectedData.length > 0) {
              GestioneCassaObj.minusClick("incr", selectedData[0]);
            }
            e.handled = true;
            break;

          default:
            break;
        }
      },
      onRowPrepared: function (e) {
        switch (e.rowType) {
          case "header":
            break;

          case "data":
            //e.rowElement.css({ height: "42px" });
            e.rowElement.find("td").css({ "vertical-align": "middle" });
            break;
        }
      },
      onInitialized(e) {
        GestioneCassaObj.gridScontrino = e.component;
      },
    });

    $("#edtTavolo").dxSelectBox({
      label: "Tavolo",
      noDataText: "-",
      showDataBeforeSearch: true,
      searchTimeout: 100,
      showDropDownButton: false,
      acceptCustomValue: true,
      searchEnabled: true,
      dataSource: GestioneCassaObj.arrayTavoli,
      onValueChanged(data) {
        if (data.value) {
          GestioneCassaObj.edtTavolo = data.value.toString().trim();
        } else {
          GestioneCassaObj.edtTavolo = "";
        }
        GestioneCassaObj.updateButtonState();
      },
    });

    $("#edtCoperti").dxSelectBox({
      label: "Coperti",
      noDataText: "-",
      showDataBeforeSearch: true,
      searchTimeout: 100,
      showDropDownButton: false,
      acceptCustomValue: true,
      searchEnabled: true,
      dataSource: GestioneCassaObj.arrayCoperti,
      onValueChanged(data) {
        if (data.value) {
          GestioneCassaObj.edtCoperti = data.value.toString().trim();
        } else {
          GestioneCassaObj.edtCoperti = "";
        }
      },
    });

    $("#edtReferente").dxTextBox({
      label: "Signor",
      inputAttr: { 'style': "text-transform: capitalize" },
      onValueChanged: function (data) {
        if (data.value) {
          let testo = data.value.trim();
          GestioneCassaObj.edtReferente = testo.charAt(0).toUpperCase() + testo.slice(1);
        } else {
          GestioneCassaObj.edtReferente = "";
        }
      },
    });

    $("#edtNotaOrdine").dxTextArea({
      label: "Nota Ordine",
      onValueChanged: function (data) {
        if (data.value) {
          GestioneCassaObj.edtNote = data.value.trim();
        } else {
          GestioneCassaObj.edtNote = "";
        }
      },
    });

    if (_Cassa.soloBanco) {
      document.getElementById('formOrdine').style.visibility = 'hidden';
      document.getElementById('formOrdine').style.height = '0px';

      document.getElementById('gridOrdine').style.height = 'calc(630px - var(--cardTitoloHeight) - var(--cardFormOrdineMargin) - var(--cardtoolbarScontrinoHeight))';
    }

    // All'inizio ripulisco lo scontrino
    GestioneCassaObj.btnCancellaOrdineOnClick();
  },

  dispose: () => {
    document.removeEventListener('keydown', GestioneCassaObj.keyHandler);
  },

  keyHandler: (e) => {
    switch (e.keyCode) {
      case 119: // FN8
        $("#edtContanti").dxNumberBox("instance").focus();

        DevExpress.data.query(GestioneCassaObj.scontrino_store._array)
          .sum("totale")
          .done(function (result) {
            $("#edtContanti").dxNumberBox("instance").option("value", Math.ceil(result / 5) * 5);
          });
        
        break;
      default:  //Fallback to default browser behaviour
        return true;
    }
    //Returning false overrides default browser event
    return false;
  },

  createButtons: (_Cassa, _Prodotti) => {
    if (_Prodotti != null) {
      GestioneCassaObj.Prodotti = _Prodotti;

      let K_ROW;
      let K_COL;

      let H_CELL;
      let W_CELL;
      let F_CELL;
      let F_LABEL;

      const numProdotti = _Prodotti.length;
      switch (true) {
        case (numProdotti <= 30):
          K_ROW = 5;
          K_COL = 6;
          F_CELL = 16;
          F_LABEL = 12;
          break;

        case (numProdotti <= 36):
          K_ROW = 6;
          K_COL = 6;
          F_CELL = 14;
          F_LABEL = 12;
          break;

        case (numProdotti <= 42):
          K_ROW = 7;
          K_COL = 6;
          F_CELL = 14;
          F_LABEL = 12;
          break;

        case (numProdotti <= 48):
          K_ROW = 8;
          K_COL = 6;
          F_CELL = 13;
          F_LABEL = 11;
          break;

        case (numProdotti <= 56):
          K_ROW = 8;
          K_COL = 7;
          F_CELL = 12;
          F_LABEL = 10;
          break;

        case (numProdotti <= 70):
          K_ROW = 10;
          K_COL = 7;
          F_CELL = 12;
          F_LABEL = 10;
          break;
      }
      H_CELL = Math.trunc((630 / K_ROW) - 2);  
      W_CELL = Math.trunc((665 / K_COL) - 2);

      console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneCassaObj - createButtons: " + H_CELL + "x" + W_CELL);

      var Index = 0;
      var table = document.createElement('table');

      table.setAttribute('class', 'table table-borderless table-responsive');
      table.setAttribute('style', 'margin-bottom: 0px;');

      for (var iRow = 0; iRow < K_ROW; iRow++) {
        var row = table.insertRow(-1);
        for (var iCol = 0; iCol < K_COL; iCol++) {
          var cell = row.insertCell(-1);
          cell.setAttribute("style", "padding:1px");

          if (Index < _Prodotti.length) {
            let prodotto = _Prodotti[Index];
            let nomeProdotto = prodotto.nomeProdotto.replaceAll("<CR>", "<br>");

            var div = document.createElement('div');
            div.setAttribute("id", "btnProdotto_" + prodotto.idProdotto);
            div.setAttribute("class", "p-0");

            let strStyle = "position:relative;overflow:hidden;font-size:" + F_CELL + "px;font-weight:bold; color:" + prodotto.foreColor + ";background-color: " + prodotto.backColor + "; width:" + W_CELL + "px; height:" + H_CELL + "px";

            if (prodotto.stato) {
              let strMagazzino = '<div title="Magazzino" style="font-size:' + F_LABEL + 'px" id="lblMagazzino_' + prodotto.idProdotto + '" class="child_top_right ' + (prodotto.magazzino > 0 ? 'visible" >' : 'invisible" >') + (prodotto.magazzino - prodotto.consumoCumulativo) + '</div>';

              let strDaEvadere = '';
              if (prodotto.viewLableDaEvadere) {
                //strDaEvadere = '<div title="Da Evadere" id="lblDaEvadere_' + prodotto.idProdotto + '" class="child_lblDaEvadere ' + (prodotto.consumo - prodotto.evaso > 0 ? 'visible" >' : 'invisible" >') + (prodotto.consumo - prodotto.evaso) + '</div>';
                strDaEvadere = '<div title="Da Evadere" style="font-size:' + F_LABEL + 'px" id="lblDaEvadere_' + prodotto.idProdotto + '" class="child_lblDaEvadere" >' + (prodotto.consumo - prodotto.evaso) + '</div>';
              }

              div.innerHTML = "<button class='btn btn-sm border p-0' style='" + strStyle + "'>" + nomeProdotto + strMagazzino + strDaEvadere + "</button>";
              div.addEventListener("click", function () { GestioneCassaObj.btnClicked(prodotto); }, false);
            } else {
              div.innerHTML = "<button class='btn btn-sm border p-0' style='" + strStyle + "' disabled>" + nomeProdotto + "</button>";
            }
            cell.append(div);
            //console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneCassaObj - createButtons 1: " + Index + "/" + _Prodotti.length);
          } else {
            var div = document.createElement('div');
            div.setAttribute("id", "btnProdottoXXX_" + Index);
            div.setAttribute("class", "p-0");

            let strStyle = "position:relative;overflow:hidden;font-size:" + F_CELL + "px;font-weight:bold; background-color: silver; width:" + W_CELL + "px; height:" + H_CELL + "px";

            div.innerHTML = "<button class='btn btn-sm border p-0' style='" + strStyle + "' disabled></button>";
            cell.append(div);

            //console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneCassaObj - createButtons 2: " + Index + "/" + _Prodotti.length);
          }
          Index++;
        }
      }
      $("#tableProdotti").empty()
      $("#tableProdotti").append(table);
    }
  },

  updateButtonState: () => {
    if (GestioneCassaObj.gridScontrino != undefined) {
      GestioneCassaObj.scontrino_store.totalCount()
        .done(function (count) {
          if (GestioneCassaObj.edtTavolo === "") {
            GestioneCassaObj.btnBanco.option("disabled", count <= 0);
            GestioneCassaObj.btnServito.option("disabled", true);
          } else {
            GestioneCassaObj.btnBanco.option("disabled", true);
            GestioneCassaObj.btnServito.option("disabled", count <= 0);
          }
          GestioneCassaObj.btnCancellaOrdine.option("disabled", count <= 0);
        })
        .fail(function (error) {
          // Handle the "error" here
          GestioneCassaObj.btnServito.option("disabled", true);
          GestioneCassaObj.btnBanco.option("disabled", true);
          GestioneCassaObj.btnCancellaOrdine.option("disabled", true);
        });
    }
  },

  btnClicked: (_prodotto) => {
    GestioneCassaObj.scontrino_store.byKey(_prodotto.idProdotto)
      .done(function (dataItem) {
        // Process the "dataItem" here
        dataItem.quantitàProdotto = dataItem.quantitàProdotto + 1;
        dataItem.totale = dataItem.prezzoUnitario * dataItem.quantitàProdotto;

        GestioneCassaObj.scontrino_store.update(dataItem.idProdotto, dataItem);
      })
      .fail(function (error) {
        // Devo aggiungere un nuovo prodotto
        GestioneCassaObj.scontrino_store.insert({
          idProdotto: _prodotto.idProdotto,
          nomeProdotto: _prodotto.nomeProdotto.replaceAll("<CR>", " "),
          quantitàProdotto: 1,
          totale: _prodotto.prezzoUnitario,
          prezzoUnitario: _prodotto.prezzoUnitario
        }).done(function (dataObj, key) {
          // Process the key and data object here
        })
          .fail(function (error) {
            // Handle the "error" here
          });
      });
  },

  btnCancellaOrdineOnClick: () => {
    GestioneCassaObj.scontrino_store.clear();

    GestioneCassaObj.edtTavolo = "";
    GestioneCassaObj.edtCoperti = "";
    GestioneCassaObj.edtReferente = "";
    GestioneCassaObj.edtNote = "";

    $("#edtTavolo").dxSelectBox("instance").reset();
    $("#edtCoperti").dxSelectBox("instance").reset();
    $("#edtReferente").dxTextBox("instance").reset();
    $("#edtNotaOrdine").dxTextArea("instance").reset();

    $("#edtTavolo").dxSelectBox("instance").focus();

    GestioneCassaObj.gridScontrino.refresh();
    GestioneCassaObj.updateButtonState();

    //GestioneCassaObj.edtContanti.option("value", 0);
    $("#edtContanti").dxNumberBox("instance").option("value", 0);
    GestioneCassaObj.calcolaResto();
  },

  btnSaveToMySQL: (strTipoOrdine) => {
    var righeOrdine = GestioneCassaObj.scontrino_store._array;

    DevExpress.data.query(righeOrdine)
      .filter(["quantitàProdotto", ">", 0])
      .count()
      .done(function (result) {
        if (result > 0) {
          GestioneCassaObj.btnServito.option("disabled", true);
          GestioneCassaObj.btnBanco.option("disabled", true);
          GestioneCassaObj.btnCancellaOrdine.option("disabled", true);

          GestioneCassaObj.objRef.invokeMethodAsync("OnSaveToMySQLAsync",
            GestioneCassaObj.flagStampaAbilitata,
            strTipoOrdine,
            GestioneCassaObj.edtTavolo,
            GestioneCassaObj.edtCoperti,
            GestioneCassaObj.edtNote,
            GestioneCassaObj.edtReferente,
            righeOrdine)
            .then(_idUltimoOrdine => {
              GestioneCassaObj.idUltimoOrdine = _idUltimoOrdine;

              GestioneCassaObj.btnRistampaOrdine.option("disabled", ((_idUltimoOrdine === 0) || (!GestioneCassaObj.flagStampaAbilitata))); 

              let _resto = $("#edtResto").dxNumberBox("instance").option("value");
              if (_resto > 0) {
                DevExpress.ui.notify(
                  {
                    message: "Ricordati di dare il resto (" + $("#edtResto").dxNumberBox("instance").option("text") + ")",
                    height: 60,
                    width: 240,
                    minWidth: 240,
                    type: 'info',
                    displayTime: 3000,
                  //  animation: {
                  //    show: { type: 'fade', duration: 400, from: 0, to: 1,},
                  //    hide: { type: 'fade', duration: 40, to: 0 },
                  //  },
                  },
                  {
                    position: 'center',
                    direction: 'left-push',
                  });
              }
              GestioneCassaObj.btnCancellaOrdineOnClick();
            });
        } else {
          GestioneCassaObj.btnCancellaOrdineOnClick();
          DevExpress.ui.notify("Nessuna riga valida - Ordine Annullato", "error", 2500);
        }
      });
  },

  updateStatoProdotti: (_Prodotti) => {
    _Prodotti.forEach(prodotto => {
      $('#lblMagazzino_' + prodotto.idProdotto).text(prodotto.magazzino - prodotto.consumoCumulativo);
      $('#lblDaEvadere_' + prodotto.idProdotto).text(prodotto.consumo - prodotto.evaso);
    });
  },

  updateAnagrProdotti: (_Cassa, _Prodotti) => {
    GestioneCassaObj.createButtons(_Cassa, _Prodotti);

    var righeOrdine = GestioneCassaObj.scontrino_store._array;
    DevExpress.data.query(righeOrdine)
      .filter(["quantitàProdotto", ">", 0])
      .count()
      .done(function (result) {
        if (result > 0) {
          DevExpress.ui.notify("Variazione Anagrafica Prodotti - Verificare l'ordine in corso", "warning", 4000);
        } else {
          DevExpress.ui.notify("Variazione Anagrafica Prodotti", "success", 4000);
        }
      });
   },

  updateDataOra: (_adesso) => {
    $('#lblDataOra').text(_adesso);
  },

  calcolaResto: () => {
    let _contanti = $("#edtContanti").dxNumberBox("instance").option("value");

    if (_contanti > 0) {
      DevExpress.data.query(GestioneCassaObj.scontrino_store._array)
        .sum("totale")
        .done(function (result) {
          $("#edtResto").dxNumberBox("instance").option("value", _contanti - result); 
        });
    } else {
      $("#edtResto").dxNumberBox("instance").option("value", 0); 
    }
  },

  minusClick: (_azione, _prodotto) => {
    GestioneCassaObj.scontrino_store.byKey(_prodotto.idProdotto)
      .done(function (dataItem) {
        // Process the "dataItem" here
        switch (_azione) {
          case "delete":
            GestioneCassaObj.scontrino_store.remove(dataItem.idProdotto);
            break;

          case "decr":
            dataItem.quantitàProdotto = dataItem.quantitàProdotto - 1;
            if (dataItem.quantitàProdotto < 0) {
              GestioneCassaObj.scontrino_store.remove(dataItem.idProdotto);
            } else {
              dataItem.totale = dataItem.prezzoUnitario * dataItem.quantitàProdotto;
              GestioneCassaObj.scontrino_store.update(dataItem.idProdotto, dataItem);
            }
            break;

          case "incr":
            dataItem.quantitàProdotto = dataItem.quantitàProdotto + 1;
            dataItem.totale = dataItem.prezzoUnitario * dataItem.quantitàProdotto;
            GestioneCassaObj.scontrino_store.update(dataItem.idProdotto, dataItem);
            break;

          default:
            break;
        }
      })
      .fail(function (error) {
        // Handle the "error" here
      });
  },

}

