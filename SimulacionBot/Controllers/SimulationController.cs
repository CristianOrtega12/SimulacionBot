using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using SimulacionBot.Models;
using SimulacionBot.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using LicenseContext = OfficeOpenXml.LicenseContext;

namespace SimulacionBot.Controllers
{
    public class SimulationController : Controller
    {
        private DBSimulacionBotEntities db = new DBSimulacionBotEntities();

        // GET: Simulation
        public ActionResult Index()
        {
            return View(db.DT_Mensajes_Telefonica_DigitalBlue.Where(x => x.Estado == false).ToList());
        }
        // GET: Borrar Datos
        public ActionResult Delete()
        {
            return View(db.DT_Mensajes_Telefonica_DigitalBlue.Where(x => x.Estado==false).ToList());
        }
        
        [HttpPost]
        public ActionResult DeleteR()
        {
            var activos = db.DT_Mensajes_Telefonica_DigitalBlue.Where(x => x.Estado == false);
            db.DT_Mensajes_Telefonica_DigitalBlue.RemoveRange(activos);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult DeleteAll()
        {
            var activos = db.DT_Mensajes_Telefonica_DigitalBlue.Where(x => x.Estado == false);
            var vacio = activos.Count();
            if (vacio>0)
            {
                db.DT_Mensajes_Telefonica_DigitalBlue.RemoveRange(activos);
                db.SaveChanges();
            }
               
            
            return RedirectToAction("Index");
        }

        public void Descargar()
        {
            var lista = db.DT_Mensajes_Telefonica_DigitalBlue.Where(x=> x.Estado==false).ToList();
            var listaDT = ConvertListToDatatable.ToDataTable(lista);
            //listaDT.Columns.Remove("__v");
            listaDT.Columns.Remove("Id");
            listaDT.Columns.Remove("CreatedAt");
            listaDT.Columns.Remove("Estado");
            listaDT.Columns.Remove("UpdateAt");
            listaDT.Columns.Remove("EstadoCliente");
            listaDT.Columns.Remove("TipoBase");
            listaDT.Columns.Remove("Particion");
            //listaDT.Columns.Remove("Id");
            Response.Clear();
            Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            Response.AddHeader("content-disposition", "attachment;filename=" + HttpUtility.UrlEncode("ReportesClientes.xlsx", System.Text.Encoding.UTF8));

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            //ExcelPackage.LicenseContext = System.ComponentModel.LicenseContext.NonCommercial;
            using (ExcelPackage pck = new ExcelPackage())
            {
                ExcelWorksheet ws = pck.Workbook.Worksheets.Add("Clientes");

                ws.Cells["A1"].LoadFromDataTable(listaDT, true);
                ws.Cells["A1"].Value = "ServiceNumber";
                ws.Cells["B1"].Value = "Observacion";
                ws.Cells["C1"].Value = "Cliente";
                ws.Cells["D1"].Value = "Departamento";
                ws.Cells["E1"].Value = "Ciudad";
                var ms = new System.IO.MemoryStream();
                pck.SaveAs(ms);
                ms.WriteTo(Response.OutputStream);
            }

            Response.Flush();
            ViewBag.Mensaje = "Borrados Con Éxito";
            Response.End();
        }
        public ActionResult Import (HttpPostedFileBase file)
        {
            var list = new List<DT_Mensajes_Telefonica_DigitalBlue>();
            if (file == null || !file.FileName.Contains("xlsx"))
            {
                ViewData["Mensaje"] = "Archivo no encontrado o no seleccionado intenta de nuevo";
                db.SaveChanges();
                return View("Index", db.DT_Mensajes_Telefonica_DigitalBlue.Where(x => x.Estado == false).ToList());
            }
            using (var package = new ExcelPackage(file.InputStream))
            {

                // get the first worksheet in the workbook
                int tipoBase = 1;
                if (file.FileName.Contains("4-72"))
                {
                    tipoBase = 472;
                }
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                int col = 1;

                for (int row = 2; worksheet.Cells[row, col].Value != null; row++)
                {
                    // do something with worksheet.Cells[row, col].Value
                    list.Add(new DT_Mensajes_Telefonica_DigitalBlue
                    {
                        ServiceNumber = worksheet.Cells[row, 2].Value.ToString().Trim(),
                        Observaciones = worksheet.Cells[row, 3].Value.ToString().Trim(),
                        Cliente = worksheet.Cells[row, 1].Value.ToString().Trim(),
                        Departamento = worksheet.Cells[row, 5].Value.ToString().Trim(),
                        Ciudad = worksheet.Cells[row, 4].Value.ToString().Trim(),
                        CreatedAt = DateTime.Now,
                        Estado=false,
                        TipoBase= tipoBase
                    });
                }
            } // the using 
            if (list.Count()>0)
            {
                db.DT_Mensajes_Telefonica_DigitalBlue.AddRange(list);
                db.SaveChanges();
                ViewData["MensajeCarga"] = "Carga Éxitosa";
            }
            return View("Index",list);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
